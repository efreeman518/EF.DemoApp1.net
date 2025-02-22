Benchmark tests
- build release version of the solution
- command prompt
  - navigate to the Test.Benchmarks release output dir
  - dotnet Test.Benchmarks.dll
- reports in in the output dir \BenchmarkDotNet.Artifacts\results


Load tests
- run release version of the solution
- command prompt
  - navigate to the Test.Load release output dir
  - dotnet Test.Load.dll
- reports in in the output dir \reports


Powershell/cmd
$solutionPath = "[Path to .sln]"   
$benchmarkPath = "Path to Test.Benchmarks project output folder - \bin\Release\net9.0"
$loadPath = "Path to Test.Load project output folder - \bin\Release\net9.0"
cd $solutionPath
dotnet build -c release   
cd $benchmarkPath   
dotnet Test.Benchmarks.dll  - see output
[VS - run the app]
cd $loadPath    
dotnet Test.Load.dll  