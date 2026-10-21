dotnet build
dotnet-trace collect --output trace.nettrace -- dotnet ./bin/Debug/net11.0/vibrissa.dll
dotnet-trace convert trace.nettrace --format speedscope
mv trace.speedscope.json trace.speedscope
rm trace.nettrace
