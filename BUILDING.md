# Building
* You will need Visual Studio 2022+ installed to build this solution.
* Access to nuget.org is needed to fetch and restore package dependencies. Please note the terms of usage for the following dependency files:
    * symsrv.dll and dbghelp.dll (originally part of the x64 / AMD64 Windows Debugger package, part of Windows SDK and many other tools) are used under the terms published [here](https://docs.microsoft.com/en-us/legal/windows-sdk/redist#debugging-tools-for-windows).
    * The DIA SDK files - msdia140.dll and msdia140.dll.manifest - are components of Visual Studio 2022 used under the terms as published [here](https://docs.microsoft.com/en-us/visualstudio/releases/2022/redistribution).
    * [XELite](https://www.nuget.org/packages/Microsoft.SqlServer.XEvent.XELite/) is used for importing Microsoft SQL Extended Event (XEL) files.
    * Other packages from Microsoft .NET family are used as well.
* Tests are implemented using [MSTest v2](https://docs.microsoft.com/en-us/visualstudio/test/mstest-update-to-mstestv2?view=vs-2022#why-upgrade-to-mstestv2). Please try to ensure all the tests are passing before submitting a PR.
* Prior to running tests, you need to execute the [downloadsyms.ps1](./Tests/TestCases/downloadsyms.ps1) file once as shown below:
``` cmd
cd .\SQLCallStackResolver\Tests\TestCases
powershell < .\downloadsyms.ps1
```
  Monitor for any warnings shown by the script and address them if needed.
* When a Pull Request (PR) is submitted for this project, there is a [GitHub Actions workflow](./.github/workflows/build.yml) which will build the project and run tests. PRs cannot merge till the workflow succeeds.
