dotnet build Frontend/
dotnet build Backend/
Start-Process dotnet -ArgumentList "run --project Frontend/."
Start-Process dotnet -ArgumentList "run --project Backend/."