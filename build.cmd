dotnet workload restore src/Hosts/Monitoring/Maui.Prometheus.Viewer/Maui.Prometheus.Viewer.csproj
dotnet restore src/Messaging.slnx --verbosity minimal
dotnet build src/Messaging.slnx --configuration Release --no-restore --verbosity minimal

