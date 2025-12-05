using Microsoft.ML;
using Microsoft.ML.Data;
using BankMarketingDashboard.Data;
using BankMarketingDashboard.Models;

namespace BankMarketingDashboard.Services
{
    public class MLPredictionService
    {
        private readonly MLContext _mlContext;
        private ITransformer? _model;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<MLPredictionService> _logger;
        private readonly string _modelPath;

        public MLPredictionService(
            IServiceScopeFactory scopeFactory,
            ILogger<MLPredictionService> logger,
            IWebHostEnvironment env)
        {
            _mlContext = new MLContext(seed: 42);
            _scopeFactory = scopeFactory;
            _logger = logger;
            _modelPath = Path.Combine(env.ContentRootPath, "MLModels", "campaign_model.zip");

            // Crear carpeta si no existe
            Directory.CreateDirectory(Path.GetDirectoryName(_modelPath)!);
        }

        public async Task InitializeAsync()
        {
            if (File.Exists(_modelPath))
            {
                _logger.LogInformation("Cargando modelo existente desde {Path}", _modelPath);
                _model = _mlContext.Model.Load(_modelPath, out _);
            }
            else
            {
                _logger.LogInformation("Modelo no encontrado. Entrenando nuevo modelo...");
                await TrainModelAsync();
            }
        }

        public async Task TrainModelAsync()
        {
            _logger.LogInformation("Iniciando entrenamiento del modelo...");

            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Cargar datos de la base de datos
            var campaignData = await Task.Run(() => dbContext.CampaignData.ToList());

            _logger.LogInformation("Registros cargados: {Count}", campaignData.Count);

            // Crear clase intermedia para ML.NET con bool label
            var trainingDataList = campaignData.Select(c => new
            {
                Label = c.Y.ToLower() == "yes", // Boolean directo
                Age = (float)c.Age,
                Job = c.Job,
                Marital = c.Marital,
                Education = c.Education,
                Default = c.Default,
                Housing = c.Housing,
                Loan = c.Loan,
                Contact = c.Contact,
                Month = c.Month,
                DayOfWeek = c.DayOfWeek,
                Duration = (float)c.Duration,
                Campaign = (float)c.Campaign,
                Pdays = (float)c.Pdays,
                Previous = (float)c.Previous,
                Poutcome = c.Poutcome,
                EmpVarRate = (float)c.EmpVarRate,
                ConsPriceIdx = (float)c.ConsPriceIdx,
                ConsConfIdx = (float)c.ConsConfIdx,
                Euribor3m = (float)c.Euribor3m,
                NrEmployed = (float)c.NrEmployed
            }).ToList();

            var dataView = _mlContext.Data.LoadFromEnumerable(trainingDataList);

            // Pipeline simplificado - Label ya es bool
            var pipeline = _mlContext.Transforms.Categorical.OneHotEncoding("JobEncoded", "Job")
                .Append(_mlContext.Transforms.Categorical.OneHotEncoding("MaritalEncoded", "Marital"))
                .Append(_mlContext.Transforms.Categorical.OneHotEncoding("EducationEncoded", "Education"))
                .Append(_mlContext.Transforms.Categorical.OneHotEncoding("DefaultEncoded", "Default"))
                .Append(_mlContext.Transforms.Categorical.OneHotEncoding("HousingEncoded", "Housing"))
                .Append(_mlContext.Transforms.Categorical.OneHotEncoding("LoanEncoded", "Loan"))
                .Append(_mlContext.Transforms.Categorical.OneHotEncoding("ContactEncoded", "Contact"))
                .Append(_mlContext.Transforms.Categorical.OneHotEncoding("MonthEncoded", "Month"))
                .Append(_mlContext.Transforms.Categorical.OneHotEncoding("DayOfWeekEncoded", "DayOfWeek"))
                .Append(_mlContext.Transforms.Categorical.OneHotEncoding("PoutcomeEncoded", "Poutcome"))
                .Append(_mlContext.Transforms.Concatenate("Features",
                    "Age", "JobEncoded", "MaritalEncoded", "EducationEncoded",
                    "DefaultEncoded", "HousingEncoded", "LoanEncoded",
                    "ContactEncoded", "MonthEncoded", "DayOfWeekEncoded",
                    "Duration", "Campaign", "Pdays", "Previous",
                    "PoutcomeEncoded", "EmpVarRate", "ConsPriceIdx",
                    "ConsConfIdx", "Euribor3m", "NrEmployed"))
                .Append(_mlContext.Transforms.NormalizeMinMax("Features"))
                .Append(_mlContext.BinaryClassification.Trainers.LbfgsLogisticRegression(
                    labelColumnName: "Label",
                    featureColumnName: "Features"));

            _logger.LogInformation("Entrenando modelo...");
            _model = pipeline.Fit(dataView);

            // Guardar modelo
            _mlContext.Model.Save(_model, dataView.Schema, _modelPath);
            _logger.LogInformation("Modelo guardado en {Path}", _modelPath);

            // Evaluar modelo
            var predictions = _model.Transform(dataView);
            var metrics = _mlContext.BinaryClassification.Evaluate(predictions, labelColumnName: "Label");

            _logger.LogInformation("Métricas del modelo:");
            _logger.LogInformation("  Accuracy: {Accuracy:P2}", metrics.Accuracy);
            _logger.LogInformation("  AUC: {Auc:F4}", metrics.AreaUnderRocCurve);
            _logger.LogInformation("  F1 Score: {F1:F4}", metrics.F1Score);
        }


        public PredictionResult Predict(PredictionInput input)
        {
            if (_model == null)
            {
                throw new InvalidOperationException("El modelo no está inicializado. Llame a InitializeAsync primero.");
            }

            var predictionEngine = _mlContext.Model.CreatePredictionEngine<PredictionInput, PredictionOutput>(_model);
            var prediction = predictionEngine.Predict(input);

            var probability = (double)prediction.Probability;
            string classification;
            string message;

            if (probability < 0.33)
            {
                classification = "Baja";
                message = $"Con una probabilidad del {probability:P1}, es poco probable que el cliente acepte el producto.";
            }
            else if (probability < 0.66)
            {
                classification = "Media";
                message = $"Con una probabilidad del {probability:P1}, existe una posibilidad moderada de que el cliente acepte.";
            }
            else
            {
                classification = "Alta";
                message = $"Con una probabilidad del {probability:P1}, es muy probable que el cliente acepte el producto.";
            }

            return new PredictionResult
            {
                Probability = probability,
                Classification = classification,
                Message = message,
                WillAccept = prediction.Prediction
            };
        }
    }
}
