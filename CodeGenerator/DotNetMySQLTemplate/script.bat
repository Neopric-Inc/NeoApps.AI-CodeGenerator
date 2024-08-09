
echo off

rem Set the path to the C# solution file
set "solutionFilePath=.\Output\solution\myeshopAPI\myeshopAPI.sln"

rem Set the working directory for the solution file
set "workingDirectory=.\"

set "outputFile=.\output.txt"

rem Set the path to the msbuild.exe executable
set "msbuildPath=C:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe"
dotnet restore %solutionFilePath%
dotnet build %solutionFilePath%
dotnet run --project %solutionFilePath% > %outputFile%


rem Get the exit code of the process
set "exitCode=%ERRORLEVEL%"

if %exitCode% neq 0 (
    echo Build failed with exit code %exitCode%
    rem Read the output file and print the contents
    type %outputFile%
) else (
    echo Build succeeded
)
