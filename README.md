[![Build SQLCallStackResolver](https://github.com/microsoft/SQLCallStackResolver/actions/workflows/build.yml/badge.svg)](https://github.com/microsoft/SQLCallStackResolver/actions/workflows/build.yml)
[![CodeQL](https://github.com/microsoft/SQLCallStackResolver/actions/workflows/codeql-analyze.yml/badge.svg)](https://github.com/microsoft/SQLCallStackResolver/actions/workflows/codeql-analyze.yml)
# Overview
SQLCallStackResolver is a sample tool provided for users who want to resolve the raw call stack information provided by Microsoft SQL Server, to a "symbolized" form with function names. This helps in self-service diagnostics of certain SQL Server issues. Please note that this sample tool is provided AS-IS - see [SUPPORT](SUPPORT.md) for details.

# Installation
Please refer to the [Releases](../../releases) section for a ready-to-run set of binaries. This sample tool does not have any other requirements other than .NET Framework 4.7.2 or above. SQLCallStackResolver requires Windows 10, Windows Server 2016, or higher. Other Microsoft components used, and their associated terms of use are documented in the [Building](./BUILDING.md) file.

# Usage
For a practical example of how SQLCallStackResolver can be used to quickly investigate hard problems, see Bob Ward's demos in this [Data Exposed episode](https://www.youtube.com/watch?v=Vw86u05SDjc).

SQLCallStackResolver takes raw call stack text as input. These call stacks can be obtained from a variety of SQL Server sources:

* Individual call stack extracted from XML output from the XEvents DMVs or .XEL files. These represent individual call stack frames in the with `module+offset` notation
* Multiple call stacks in Histogram XML markup (multiple-instance case of above).
* The newer XML format provided by the `callstack_rva` action in SQL Server 2022 and above. [[example]](#usage-example-3)
* Older format with just virtual addresses (see notes). This is typically what you would get when viewing a .XEL file (containing call stack events) using SQL Server Management Studio.
* Output from SQLDumper (SQLDumpNNNN.TXT file) - at least the sections which have stack frames (see notes).
* `dll!OrdinalNNN` frames (see notes). This is a very rare case and typically an outcome of user error (for example, enabling a trace flag to symbolize call stacks, without having the required PDBs).

The tool has two modes of operation. Usually, you'll want to use the Simple mode, which offers a clean, wizard based approach, with helpful prompting where relevant. Here are some examples of how to use this mode.

## Usage example #1
This is a simple case, where you enter the call stack in `module+offset` notation, and select one of the pre-populated list of SQL builds (versions) to download symbols. You then click the Resolve Callstacks button, and obtain the symbolized output in the right-hand side textbox.
![](images/1_ModOffset_Text.gif)

If the input is in the `module+offset` format, and if a SQL Server build (version) number is also in the file, such as is the case with SQLDump*.txt files, the tool helpfully asks if you want to use symbols for that version.
![](images/1A_ModOffset_DumpTxt.gif)

## Usage example #2
This is a typical use case, where you can import events from a XEL file. Because the XEL file does not have the module base addresses, you have to first provide those module base addresses. You can then select one of the pre-populated list of SQL builds (versions) to download symbols and finally click the Resolve Callstacks button to obtain the symbolized output in the right-hand side textbox.
![](images/2_XEL_Address.gif)

## Usage example #3
With SQL Server 2022 and above, call stacks returned by the XE functions are represented in a XML format, with PDB symbol information provided inline for each frame. With this format for call stacks, you can alternatively specify the symbol path as a symbol server, as documented in the [WinDbg help](https://docs.microsoft.com/en-us/windows-hardware/drivers/debugger/symbol-path#using-a-symbol-server). An example of this is shown below.
![](images/3_SQL2022_format.gif)

## Notes on usage
1. When importing XEL files, you need to populate the Base Addresses with the output of the following query from the actual SQL instance when the XE was captured:
```sql
select name, base_address from sys.dm_os_loaded_modules where name not like '%.rll'
```
2. The symbol path has to be path to a folder or a set of such paths (can be UNC as well) each separated with a semicolon (;). Use the checkbox to specify if sub-folders need to be checked in each case. If multiple paths might contain matching PDBs, the first path from the left which contained the PDB wins. There is no means to know if the PDB is matched with the build (version) that your are using - you need to ensure that the folder path(s) are correct!
3. To obtain public PDBs for major SQL releases, PowerShell scripts are available in the SQLCallStackResolver [Wiki](https://github.com/arvindshmicrosoft/SQLCallStackResolver/wiki/Obtaining-symbol-files-(.PDB)-for-SQL-Server-Releases)
4. When dealing with SQLDump*.txt files, the tool does not strip out just the 'Short Stack Dump' sections; instead it will preserve non-callstack text as-is.
5. With callstack output from SQL Server 2022 and above, it is possible to use a symbol server in the symbol search path. For example, the symbol search path can be specified as `srv*c:\syms*https://msdl.microsoft.com/download/symbols`.
6. When dealing with frames having `OrdinalNNN`, you need to press the Module Paths button where you will be prompted to enter the path to a folder containing the modules (typically, DLLs) involved. For example you can point to C:\Program Files\Microsoft SQL Server\MSSQL15.MSSQLSERVER\MSSQL\Binn for SQL Server 2019.

# Contributing
This project welcomes contributions and suggestions. Most contributions require you to agree to a Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com. When you submit a pull request, a CLA bot will automatically determine whether you need to provide a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions provided by the bot. You will only need to do this once across all repos using our CLA. For technical instructions on how to build and test this project, see [Building](./BUILDING.md).

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

# Trademarks
This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft trademarks or logos is subject to and must follow [Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/en-us/legal/intellectualproperty/trademarks/usage/general). Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship. Any use of third-party trademarks or logos are subject to those third-party's policies.
