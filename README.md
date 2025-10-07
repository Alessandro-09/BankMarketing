# BankMarketing Dashboard

Dashboard para análisis de campañas bancarias (SRS v1.5).

## Requisitos
- .NET 9 SDK
- SQL Server 2022 Developer
- Visual Studio 2022

## Instrucciones de instalación

1. Clonar el repositorio
3. Crear base de datos `BankMarketingDB` en SQL Server.
4. Ejecutar la consulta de `ScriptsSQL/Table_CampaignData` dentro de la base de datos.
5. Ejecutar la conuslta de importar datos con `ScriptsSQL/Import_csv` (ajustar ruta del CSV). En mi caso utilicé bank-additional-full.csv
6. Abrir `BankMarketingDashboard.sln` en Visual Studio y ejecutar.
