// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Xml;

namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver {
    /// <summary>
    /// Class implementing tests.
    /// </summary>
    [TestClass]
    public class Tests {
        [TestMethod][TestCategory("Unit")] public void SingleLineDetection() {
            using var csr = new StackResolver();
            var PatternsToTreatAsMultiline = "BEGIN STACK DUMP|Short Stack Dump";
            Assert.IsTrue(csr.IsInputSingleLine(@"callstack	          0x00007FFEABD0D919        0x00007FFEABC4D45D      0x00007FFEAC0F7EE0  0x00007FFEAC0F80CF  0x00007FFEAC1EE447  0x00007FFEAC1EE6F5  0x00007FFEAC1D48B0  0x00007FFEAC71475A  0x00007FFEA9A708F1  0x00007FFEA9991FB9  0x00007FFEA9993D21  0x00007FFEA99B59F1  0x00007FFEA99B5055  0x00007FFEA99B2B8F  0x00007FFEA9675AD1  0x00007FFEA9671EFB  0x00007FFEAA37D83D  0x00007FFEAA37D241  0x00007FFEAA379F98  0x00007FFEA96719CA  0x00007FFEA9672933  0x00007FFEA9672041  0x00007FFEA967A82B  0x00007FFEA9681542  ", PatternsToTreatAsMultiline));
            Assert.IsTrue(csr.IsInputSingleLine(@"callstack	          0x00007FFEABD0D919        0x00007FFEABC4D45D      0x00007FFEAC0F7EE0  0x00007FFEAC0F80CF  0x00007FFEAC1EE447  0x00007FFEAC1EE6F5  0x00007FFEAC1D48B0  0x00007FFEAC71475A  0x00007FFEA9A708F1  0x00007FFEA9991FB9  0x00007FFEA9993D21\r\n0x00007FFEA99B59F1  0x00007FFEA99B5055  0x00007FFEA99B2B8F  0x00007FFEA9675AD1  0x00007FFEA9671EFB  0x00007FFEAA37D83D  0x00007FFEAA37D241  0x00007FFEAA379F98  0x00007FFEA96719CA  0x00007FFEA9672933  0x00007FFEA9672041  0x00007FFEA967A82B\r\n0x00007FFEA9681542  ", PatternsToTreatAsMultiline));
            Assert.IsTrue(csr.IsInputSingleLine(@"\r\n    sqldk+0x40609 sqldk+40609\r\n", PatternsToTreatAsMultiline));
            Assert.IsTrue(csr.IsInputSingleLine(@"\r\n    sqldk+0x40609 sqldk+40609\r\nsqldk+0x40609 sqldk+40609", PatternsToTreatAsMultiline));
            Assert.IsTrue(csr.IsInputSingleLine(@"\r\ncallstack	          0x00007FFEABD0D919        0x00007FFEABC4D45D      0x00007FFEAC0F7EE0  0x00007FFEAC0F80CF  0x00007FFEAC1EE447  0x00007FFEAC1EE6F5  0x00007FFEAC1D48B0  0x00007FFEAC71475A  0x00007FFEA9A708F1  0x00007FFEA9991FB9  0x00007FFEA9993D21  0x00007FFEA99B59F1  0x00007FFEA99B5055  0x00007FFEA99B2B8F  0x00007FFEA9675AD1  0x00007FFEA9671EFB  0x00007FFEAA37D83D  0x00007FFEAA37D241  0x00007FFEAA379F98  0x00007FFEA96719CA  0x00007FFEA9672933  0x00007FFEA9672041  0x00007FFEA967A82B  0x00007FFEA9681542    \r\n\r\n", PatternsToTreatAsMultiline));
            Assert.IsTrue(csr.IsInputSingleLine("<HistogramTarget truncated=\"0\" buckets=\"256\"><Slot count=\"5\"><value>0x00007FFEABD0D919        0x00007FFEABC4D45D      0x00007FFEAC0F7EE0  0x00007FFEAC0F80CF  0x00007FFEAC1EE447  0x00007FFEAC1EE6F5  0x00007FFEAC1D48B0  0x00007FFEAC71475A  0x00007FFEA9A708F1  0x00007FFEA9991FB9  0x00007FFEA9993D21  0x00007FFEA99B59F1  0x00007FFEA99B5055  0x00007FFEA99B2B8F  0x00007FFEA9675AD1  0x00007FFEA9671EFB  0x00007FFEAA37D83D  0x00007FFEAA37D241  0x00007FFEAA379F98  0x00007FFEA96719CA  0x00007FFEA9672933  0x00007FFEA9672041  0x00007FFEA967A82B  0x00007FFEA9681542</value></Slot></HistogramTarget>", PatternsToTreatAsMultiline));
            Assert.IsTrue(csr.IsInputSingleLine("annotation for histogram #1 0 <HistogramTarget truncated=\"0\" buckets=\"256\"><Slot count=\"5\"><value>0x00007FFEABD0D919        0x00007FFEABC4D45D      0x00007FFEAC0F7EE0  0x00007FFEAC0F80CF  0x00007FFEAC1EE447  0x00007FFEAC1EE6F5  0x00007FFEAC1D48B0  0x00007FFEAC71475A  0x00007FFEA9A708F1  0x00007FFEA9991FB9  0x00007FFEA9993D21  0x00007FFEA99B59F1  0x00007FFEA99B5055  0x00007FFEA99B2B8F  0x00007FFEA9675AD1  0x00007FFEA9671EFB  0x00007FFEAA37D83D  0x00007FFEAA37D241  0x00007FFEAA379F98  0x00007FFEA96719CA  0x00007FFEA9672933  0x00007FFEA9672041  0x00007FFEA967A82B  0x00007FFEA9681542</value></Slot></HistogramTarget>\r\n" +
                "annotation for histogram #2 1 <HistogramTarget truncated=\"0\" buckets=\"256\"><Slot count=\"5\"><value>0x00007FFEABD0D919        0x00007FFEABC4D45D      0x00007FFEAC0F7EE0  0x00007FFEAC0F80CF  0x00007FFEAC1EE447  0x00007FFEAC1EE6F5  0x00007FFEAC1D48B0  0x00007FFEAC71475A  0x00007FFEA9A708F1  0x00007FFEA9991FB9  0x00007FFEA9993D21  0x00007FFEA99B59F1  0x00007FFEA99B5055  0x00007FFEA99B2B8F  0x00007FFEA9675AD1  0x00007FFEA9671EFB  0x00007FFEAA37D83D  0x00007FFEAA37D241  0x00007FFEAA379F98  0x00007FFEA96719CA  0x00007FFEA9672933  0x00007FFEA9672041  0x00007FFEA967A82B  0x00007FFEA9681542</value></Slot></HistogramTarget>\r\n", PatternsToTreatAsMultiline));
            Assert.IsFalse(csr.IsInputSingleLine("<HistogramTarget truncated=\"0\" buckets=\"256\"><Slot count=\"5\"><value><![CDATA[<frame id=\"00\" pdb=\"ntdll.pdb\" age=\"1\" guid=\"C374E059-5793-9B92-6525-386A66A2D3F5\" module=\"ntdll.dll\" rva=\"0x9F7E4\" />" +
"<frame id=\"01\" pdb=\"kernelbase.pdb\" age=\"1\" guid=\"E77E26E7-D1C4-72BB-2C05-DD17624A9E58\" module=\"KERNELBASE.dll\" rva=\"0x38973\" />" +
"<frame id=\"02\" pdb=\"SqlDK.pdb\" age=\"2\" guid=\"6a193443-3512-464b-8b8e-d905ad930ee6\" module=\"sqldk.dll\" rva=\"0x40609\" />" +
"]]></value></Slot><Slot count=\"3\"><value><![CDATA[<frame id=\"00\" pdb=\"vcruntime140.amd64.pdb\" age=\"1\" guid=\"AF138C3F-2933-4097-8883-C1071B13375E\" module=\"VCRUNTIME140.dll\" rva=\"0xB8F0\" />" +
"<frame id=\"01\" pdb=\"SqlDK.pdb\" age=\"2\" guid=\"6a193443-3512-464b-8b8e-d905ad930ee6\" module=\"sqldk.dll\" rva=\"0x2249f\" />" +
"]]></value></Slot></HistogramTarget>", PatternsToTreatAsMultiline));
            Assert.IsFalse(csr.IsInputSingleLine("<HistogramTarget truncated=\"0\" buckets=\"256\">\r\n<Slot count=\"5\">\r\n<value>&lt;frame id=\"00\" address=\"0xf00\" pdb=\"ntdll.pdb\" age=\"1\" guid=\"C374E059-5793-9B92-6525-386A66A2D3F5\" module=\"ntdll.dll\" rva=\"0x9F7E4\" /&gt;&lt;" +
"frame id=\"01\" address=\"0xf00\" pdb=\"kernelbase.pdb\" age=\"1\" guid=\"E77E26E7-D1C4-72BB-2C05-DD17624A9E58\" module=\"KERNELBASE.dll\" rva=\"0x38973\" /&gt;&lt;" +
"frame id=\"02\" address=\"0xf00\" pdb=\"SqlDK.pdb\" age=\"2\" guid=\"6a193443-3512-464b-8b8e-d905ad930ee6\" module=\"sqldk.dll\" rva=\"0x40609\" /&gt;" +
"</value>\r\n</Slot>\r\n<Slot count=\"3\">\r\n<value><frame id=\"00\" address=\"0xf00\" pdb=\"vcruntime140.amd64.pdb\" age=\"1\" guid=\"AF138C3F-2933-4097-8883-C1071B13375E\" module=\"VCRUNTIME140.dll\" rva=\"0xB8F0\" /&gt;&lt;" +
"frame id=\"01\" address=\"0xf00\" pdb=\"SqlDK.pdb\" age=\"2\" guid=\"6a193443-3512-464b-8b8e-d905ad930ee6\" module=\"sqldk.dll\" rva=\"0x2249f\" /&gt;" +
"</value>\r\n</Slot>\r\n</HistogramTarget>", PatternsToTreatAsMultiline));
            Assert.IsFalse(csr.IsInputSingleLine("\r\nThis file is generated by Microsoft SQL Server                                                                   \r\n\r\n* BEGIN STACK DUMP:                                                                                              \r\n\r\na = 0x0000000000000000                            a1 = 0x0000000000000000    \r\nb = 0x0000000000000000                                                                       \r\nc = 0x0000000000000000                           d = 0x0000000000000000 \r\n", PatternsToTreatAsMultiline));
            Assert.IsTrue(csr.IsInputSingleLine("Histogram 0x1 0x1", PatternsToTreatAsMultiline));
            Assert.IsFalse(csr.IsInputSingleLine("Histogram 0x1\r\n0x1", PatternsToTreatAsMultiline));
        }

        /// Validate that "block symbols" in a PDB are resolved correctly.
        [TestMethod][TestCategory("Unit")] public async Task BlockResolution() {
            using var csr = new StackResolver();
            using var cts = new CancellationTokenSource();
            var pdbPath = @"..\..\..\..\..\Tests\TestCases\TestBlockResolution";
            var ret = await csr.ResolveCallstacksAsync(await csr.GetListofCallStacksAsync("Return Addr: 00007FF830D4CDA4 Module(KERNELBASE+000000000009CDA4)", false, cts), pdbPath, false, null, false, false, false, true, false, false, null, cts);
            Assert.AreEqual("KERNELBASE!SignalObjectAndWait+147716", ret.Trim());
        }

        /// Test the resolution of OrdinalNNN symbols to their actual names. We throw in some frames containing non-relevant info for good measure.
        [TestMethod][TestCategory("Unit")] public async Task OrdinalBasedSymbol() {
            using var csr = new StackResolver();
            using var cts = new CancellationTokenSource();
            var dllPaths = new List<string> { Path.GetTempPath(), @"..\..\..\..\..\Tests\TestCases\TestOrdinal", Path.GetTempPath() };    // use different paths to validate the multi-path handling
            var ret = await csr.ResolveCallstacksAsync(await csr.GetListofCallStacksAsync("sqldk!Ordinal298+00000000000004A5\r\n00007FF818405E70      Module(sqldk+0000000000003505) (Ordinal298 + 00000000000004A5)", false, cts), @"..\..\..\..\..\Tests\TestCases\TestOrdinal", false, dllPaths, false, false, false, true, false, false, null, cts);
            Assert.AreEqual("sqldk!SOS_Scheduler::SwitchContext+941\r\nsqldk!SOS_Scheduler::SwitchContext+941", ret.Trim());
        }

        /// Test the resolution of a "regular" symbol with input specifying a hex offset into module.
        [TestMethod][TestCategory("Unit")] public async Task RegularSymbolHexOffset() {
            using var csr = new StackResolver();
            using var cts = new CancellationTokenSource();
            var ret = await csr.ResolveCallstacksAsync(await csr.GetListofCallStacksAsync("sqldk +0x40609\r\nsqldk+40609", false, cts), @"..\..\..\..\..\Tests\TestCases\TestOrdinal", false, null, false, false, false, true, false, false, null, cts);
            var expectedSymbol = "sqldk!MemoryClerkInternal::AllocatePagesWithFailureMode+644";
            Assert.AreEqual(expectedSymbol + Environment.NewLine + expectedSymbol, ret.Trim());
        }

        /// Test the resolution of a "regular" symbol with virtual address as input.
        [TestMethod][TestCategory("Unit")] public async Task RegularSymbolVirtualAddress() {
            using var csr = new StackResolver();
            using var cts = new CancellationTokenSource();
            Assert.IsTrue(csr.ProcessBaseAddresses(@"c:\mssql\binn\sqldk.dll 00000001`00400000"));
            var ret = await csr.ResolveCallstacksAsync(await csr.GetListofCallStacksAsync("0x000000010042249f", false, cts), @"..\..\..\..\..\Tests\TestCases\TestOrdinal", false, null, false, false, false, true, false, false, null, cts);
            var expectedSymbol = "sqldk!Spinlock<244,2,1>::SpinToAcquireWithExponentialBackoff+349";
            Assert.AreEqual(expectedSymbol, ret.Trim());
        }

        [TestMethod][TestCategory("Unit")] public async Task RegularSymbolVirtualAddressDupeModules() {
            using var csr = new StackResolver();
            using var cts = new CancellationTokenSource();
            var moduleAddresses = "c:\\mssql\\binn\\sqldk.dll 00000001`00400000\r\nc:\\windows\\temp\\sqldk.dll 00000001`00400000";
            Assert.IsFalse(csr.ProcessBaseAddresses(moduleAddresses));
            var ret = await csr.ResolveCallstacksAsync(await csr.GetListofCallStacksAsync("0x000000010042249f\r\nsqldk+0x40609", false, cts), @"..\..\..\..\..\Tests\TestCases\TestOrdinal", false, null, false, false, false, true, false, false, null, cts);
            Assert.AreEqual("0x000000010042249f\r\nsqldk!MemoryClerkInternal::AllocatePagesWithFailureMode+644", ret.Trim());
        }

        /// Perf / scale test. We randomly generate 750K XEvents each with 25 frame callstacks, and then resolve them.
        [TestMethod]
        [TestCategory("Perf")]
        public async Task LargeXEventsInput() {
            using var csr = new StackResolver();
            using var cts = new CancellationTokenSource();
            Assert.IsTrue(csr.ProcessBaseAddresses(@"c:\mssql\binn\sqldk.dll 00000001`00400000"));
            var timer = new Stopwatch();
            timer.Start();
            var input = PrepareLargeXEventInput();
            var outputFilename = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var res = await csr.ResolveCallstacksAsync(await csr.GetListofCallStacksAsync(input, false, cts), @"..\..\..\..\..\Tests\TestCases\TestOrdinal", false, null, false, false, false, true, false, false, outputFilename, cts);
            timer.Stop();
            Assert.IsTrue(timer.Elapsed.TotalSeconds < 45 * 60);  // 45 minutes max on GitHub hosted DSv2 runner (2 vCPU, 7 GiB RAM).
            Assert.IsTrue((await File.ReadAllTextAsync(outputFilename)).Length > input.Length);
        }

        private static string PrepareLargeXEventInput() {
            var xeventInput = new StringBuilder("<Events>");
            var rng = new Random();
            // generate 750K XEvents, each with 25 frames, each frame having a random address between 0000000100400000 and 00000001008c8000, which is a 5013503 byte range
            for (var eventNum = 0; eventNum < 750000; eventNum++) {
                xeventInput.AppendLine($"<event key=\"File: C:\\temp\\test.xel, Timestamp: {DateTime.UtcNow.ToLongTimeString()}, UUID: {Guid.NewGuid()}\"><action name='callstack'><value>");
                for (int frameNum = 0; frameNum < 25; frameNum++) {
                    xeventInput.AppendLine("0x" + (0x0000000100400000 + rng.Next(0, 5013503)).ToString("x2"));
                }
                xeventInput.AppendLine($"</value></action></event>");
            }
            xeventInput.AppendLine("</Events>");
            return xeventInput.ToString();
        }

        /// Check the processing of module base address information.
        [TestMethod][TestCategory("Unit")] public void ModuleLoadAddressInputNonRecognizedInput() {
            using var csr = new StackResolver();
            var b = OperatingSystem.IsWindows();
            Assert.IsTrue(csr.ProcessBaseAddresses(string.Empty));
            Assert.IsFalse(csr.ProcessBaseAddresses(@"hello wor1213ld"));
            Assert.IsFalse(csr.ProcessBaseAddresses(@"<<System32\KERNELBASE.dll>>	0x00007FFEC0700000"));
            Assert.IsFalse(csr.ProcessBaseAddresses(@"C:\System32\KERNELBASE.dll	0x00007FFEC07000007FFEC0700000"));
            Assert.IsFalse(csr.ProcessBaseAddresses(@"C:\System32\KERNELBASE.dll	0x00007FFEC0700000 0x7FFEC0700000"));
            Assert.IsFalse(csr.ProcessBaseAddresses(@"C:\Windows\System32\KERNELBASE.dll	0x0xaaa"));
        }

        /// Check the processing of module base address information.
        [TestMethod][TestCategory("Unit")] public void ModuleLoadAddressInputColHeaders() {
            using var csr = new StackResolver();
            var moduleAddressesColHeader = File.ReadAllText(@"..\..\..\..\..\Tests\TestCases\ImportXEL\xe_wait_base_addresses.txt");
            Assert.IsTrue(csr.ProcessBaseAddresses(moduleAddressesColHeader));
            var sqllang = csr.LoadedModules.Where(m => m.ModuleName == "sqllang").First();
            Assert.IsNotNull(sqllang);
            Assert.AreEqual(0x00007FFACB6F0000UL, sqllang.BaseAddress);
            Assert.AreEqual(0x00007FFAD089FFFFUL, sqllang.EndAddress);
        }

        /// Check the processing of module base address information.
        [TestMethod][TestCategory("Unit")] public void ModuleLoadAddressInputFullPathSingleLine() {
            using var csr = new StackResolver();
            Assert.IsTrue(csr.ProcessBaseAddresses(@"c:\mssql\binn\sqldk.dll 0000000100400000"));
            Assert.AreEqual(1, csr.LoadedModules.Count);
            Assert.AreEqual("sqldk", csr.LoadedModules[0].ModuleName);
        }

        /// Check the processing of module base address information.
        [TestMethod][TestCategory("Unit")] public void ModuleLoadAddressInputSingleLineBacktick() {
            using var csr = new StackResolver();
            Assert.IsTrue(csr.ProcessBaseAddresses(@"c:\mssql\binn\sqldk.dll 00000001`00400000"));
            Assert.AreEqual(1, csr.LoadedModules.Count);
            Assert.AreEqual("sqldk", csr.LoadedModules[0].ModuleName);
        }

        /// Check the processing of module base address information.
        [TestMethod][TestCategory("Unit")] public void ModuleLoadAddressInputModuleNameOnlySingleLine() {
            using var csr = new StackResolver();
            Assert.IsTrue(csr.ProcessBaseAddresses(@"sqldk.dll 0000000100400000"));
            Assert.AreEqual(1, csr.LoadedModules.Count);
            Assert.AreEqual("sqldk", csr.LoadedModules[0].ModuleName);
        }

        /// Check the processing of module base address information.
        [TestMethod][TestCategory("Unit")] public void ModuleLoadAddressInputModuleNameOnlySingleLine0x() {
            using var csr = new StackResolver();
            Assert.IsTrue(csr.ProcessBaseAddresses(@"sqldk.dll 0x0000000100400000"));
            Assert.AreEqual(1, csr.LoadedModules.Count);
            Assert.AreEqual("sqldk", csr.LoadedModules[0].ModuleName);
        }

        /// Check the processing of module base address information.
        [TestMethod][TestCategory("Unit")] public void ModuleLoadAddressInputFullPathsTwoModules() {
            using var csr = new StackResolver();
            Assert.IsTrue(csr.ProcessBaseAddresses("c:\\mssql\\binn\\sqldk.dll 0000000100400000\r\nc:\\mssql\\binn\\sqllang.dll 0000000105600000"));
            Assert.AreEqual(2, csr.LoadedModules.Count);
            Assert.AreEqual("sqldk", csr.LoadedModules[0].ModuleName);
            Assert.AreEqual("sqllang", csr.LoadedModules[1].ModuleName);
        }

        /// Test the resolution of a "regular" symbol with input specifying a hex offset into module
        /// but do not include the resolved symbol's offset in final output.
        [TestMethod][TestCategory("Unit")] public async Task RegularSymbolHexOffsetNoOutputOffset() {
            using var csr = new StackResolver();
            using var cts = new CancellationTokenSource();
            var ret = await csr.ResolveCallstacksAsync(await csr.GetListofCallStacksAsync("sqldk+0x40609", false, cts), @"..\..\..\..\..\Tests\TestCases\TestOrdinal", false, null, false, false, false, false, false, false, null, cts);
            var expectedSymbol = "sqldk!MemoryClerkInternal::AllocatePagesWithFailureMode";
            Assert.AreEqual(expectedSymbol, ret.Trim());
        }

        /// Test the resolution of a "regular" symbol with input specifying a hex offset into module
        /// but do not include the resolved symbol's offset in final output. This variant has the 
        /// frame numbers prefixed.
        [TestMethod][TestCategory("Unit")] public async Task RegularSymbolHexOffsetNoOutputOffsetWithFrameNums() {
            using var csr = new StackResolver();
            using var cts = new CancellationTokenSource();
            var ret = await csr.ResolveCallstacksAsync(await csr.GetListofCallStacksAsync("00 sqldk+0x40609", false, cts), @"..\..\..\..\..\Tests\TestCases\TestOrdinal", false, null, false, false, false, false, false, false, null, cts);
            var expectedSymbol = "00 sqldk!MemoryClerkInternal::AllocatePagesWithFailureMode";
            Assert.AreEqual(expectedSymbol, ret.Trim());
        }

        /// Check whether symbol details for a given binary are correct.
        [TestMethod][TestCategory("Unit")] public async Task GetSymDetails() {
            var dllPaths = new List<string> { @"..\..\..\..\..\Tests\TestCases\TestOrdinal" };
            var ret = await StackResolver.GetSymbolDetailsForBinaries(dllPaths, true);
            Assert.AreEqual(1, ret.Count);
            Assert.AreEqual("https://msdl.microsoft.com/download/symbols/sqldk.pdb/6a1934433512464b8b8ed905ad930ee62/sqldk.pdb", ret[0].DownloadURL);
            Assert.IsTrue(ret[0].DownloadVerified);
            Assert.AreEqual("2015.0130.4560.00 ((SQL16_SP1_QFE-CU).190312-0204)", ret[0].FileVersion);
        }

        /// Make sure that caching PDB files is working. To do this we must use XEL input to trigger multiple worker threads.
        [TestMethod][TestCategory("Unit")] public async Task SymbolFileCaching() {
            using var csr = new StackResolver();
            using var cts = new CancellationTokenSource();
            var ret = await csr.ExtractFromXELAsync(new[] { @"..\..\..\..\..\Tests\TestCases\ImportXEL\xe_wait_completed_0_132353446563350000.xel" }, false, new List<string>(new String[] { "callstack" }), cts);
            Assert.AreEqual(550, ret.Item1);
            Assert.IsTrue(csr.ProcessBaseAddresses(File.ReadAllText(@"..\..\..\..\..\Tests\TestCases\ImportXEL\xe_wait_base_addresses.txt")));
            var pdbPath = @"..\..\..\..\..\Tests\TestCases\sqlsyms\14.0.3192.2\x64";
            var symres = await csr.ResolveCallstacksAsync(await csr.GetListofCallStacksAsync(ret.Item2, false, cts), pdbPath, false, null, false, false, false, false, false, true, null, cts);
            Assert.IsTrue(symres.Contains("sqldk!XeSosPkg::wait_completed::Publish\r\nsqldk!SOS_Scheduler::UpdateWaitTimeStats\r\nsqldk!SOS_Task::PostWait\r\nsqllang!SOS_Task::Sleep\r\nsqllang!YieldAndCheckForAbort\r\nsqllang!OptimizerUtil::YieldAndCheckForMemoryAndAbort\r\nsqllang!OptTypeVRSetArray::IFindSet\r\nsqllang!CConstraintProp::FEquivalent\r\nsqllang!CJoinEdge::FConstrainsColumnSolvably\r\nsqllang!CStCollOuterJoin::CardForColumns\r\nsqllang!CStCollGroupBy::CStCollGroupBy\r\nsqllang!CCardFrameworkSQL12::CardDistinct\r\nsqllang!CCostUtils::CalcLoopJoinCachedInfo\r\nsqllang!CCostUtils::PcctxLoopJoinHelper\r\nsqllang!COpArg::PcctxCalculateNormalizeCtx\r\nsqllang!CTask_OptInputs::Perform\r\nsqllang!CMemo::ExecuteTasks\r\nsqllang!CMemo::PerformOptimizationStage\r\nsqllang!CMemo::OptimizeQuery\r\nsqllang!COptContext::PexprSearchPlan\r\nsqllang!COptContext::PcxteOptimizeQuery\r\nsqllang!COptContext::PqteOptimizeWrapper\r\nsqllang!PqoBuild\r\nsqllang!CStmtQuery::InitQuery"));
        }

        /// Validate that source information is retrieved correctly. This test uses symbols for a Windows Driver Kit module, Wdf01000.sys,
        /// because private PDBs for that module are legitimately available on the Microsoft public symbols servers. https://github.com/microsoft/Windows-Driver-Frameworks/releases if interested.
        [TestMethod][TestCategory("Unit")] public async Task SourceInformation() {
            using var csr = new StackResolver();
            using var cts = new CancellationTokenSource();
            var pdbPath = @"..\..\..\..\..\Tests\TestCases\SourceInformation";
            var ret = await csr.ResolveCallstacksAsync(await csr.GetListofCallStacksAsync("Wdf01000+17f27", false, cts), pdbPath, false, null, false, true, false, true, false, false, null, cts);
            Assert.AreEqual("Wdf01000!FxPkgPnp::PowerPolicyCanChildPowerUp+143\t(minkernel\\wdf\\framework\\shared\\inc\\private\\common\\FxPkgPnp.hpp:4127)", ret.Trim());
        }

        /// Validate that source information is retrieved correctly.This test uses symbols for a Windows Driver Kit module, Wdf01000.sys,
        /// because private PDBs for that module are legitimately available on the Microsoft public symbols servers. https://github.com/microsoft/Windows-Driver-Frameworks/releases if interested.
        [TestMethod][TestCategory("Unit")] public async Task SourceInformationLineInfoOff() {
            using var csr = new StackResolver();
            using var cts = new CancellationTokenSource();
            var pdbPath = @"..\..\..\..\..\Tests\TestCases\SourceInformation";
            var ret = await csr.ResolveCallstacksAsync(await csr.GetListofCallStacksAsync("Wdf01000+17f27", false, cts), pdbPath, false, null, false, false, false, true, false, false, null, cts);
            Assert.AreEqual("Wdf01000!FxPkgPnp::PowerPolicyCanChildPowerUp+143", ret.Trim());
        }

        /// Validate that source information is retrieved correctly when "re-looking up" a symbol based on input which was already symbolized (but missing source info).
        /// This test uses symbols for a Windows Driver Kit module, Wdf01000.sys, because private PDBs for that module are legitimately available on the Microsoft public symbols servers.https://github.com/microsoft/Windows-Driver-Frameworks/releases if interested.
        [TestMethod][TestCategory("Unit")] public async Task RelookupSourceInformation() {
            using var csr = new StackResolver();
            using var cts = new CancellationTokenSource();
            var pdbPath = @"..\..\..\..\..\Tests\TestCases\SourceInformation";
            var ret = await csr.ResolveCallstacksAsync(await csr.GetListofCallStacksAsync("Wdf01000!FxPkgPnp::PowerPolicyCanChildPowerUp+143", false, cts), pdbPath, false, null, false, true, true, true, false, false, null, cts);
            Assert.AreEqual("Wdf01000!FxPkgPnp::PowerPolicyCanChildPowerUp+143\t(minkernel\\wdf\\framework\\shared\\inc\\private\\common\\FxPkgPnp.hpp:4127)", ret.Trim());
        }

        /// Validate importing callstack events from XEL files into histogram buckets.
        [TestMethod][TestCategory("Unit")] public async Task ImportBinResolveXELEvents() {
            using var csr = new StackResolver();
            using var cts = new CancellationTokenSource();
            var ret = await csr.ExtractFromXELAsync(new[] { @"..\..\..\..\..\Tests\TestCases\ImportXEL\XESpins_0_131627061603030000.xel" }, true, new List<string>(new String[] { "callstack" }), cts);
            Assert.AreEqual(4, ret.Item1);

            var xmldoc = new XmlDocument() { XmlResolver = null };
            bool isXMLdoc = false;
            try {
                using var sreader = new StringReader(ret.Item2);
                using var reader = XmlReader.Create(sreader, new XmlReaderSettings() { XmlResolver = null });
                xmldoc.Load(reader);
                isXMLdoc = true;
            } catch (XmlException) { /* do nothing because this is not a XML doc */ }

            Assert.IsTrue(isXMLdoc);
            var slotNodes = xmldoc.SelectNodes("/HistogramTarget/Slot");
            Assert.AreEqual(4, slotNodes?.Count);
            int eventCountFromXML = 0;
            foreach (XmlNode slot in slotNodes) {
                eventCountFromXML += int.Parse(slot.Attributes["count"].Value, CultureInfo.CurrentCulture);
            }

            Assert.AreEqual(3051540, eventCountFromXML);
            csr.ProcessBaseAddresses(File.ReadAllText(@"..\..\..\..\..\Tests\TestCases\ImportXEL\base_addresses.txt"));
            Assert.AreEqual(20, csr.LoadedModules.Count);
            var pdbPath = @"..\..\..\..\..\Tests\TestCases\sqlsyms\13.0.4001.0\x64";
            var symres = await csr.ResolveCallstacksAsync(await csr.GetListofCallStacksAsync(ret.Item2, false, cts), pdbPath, false, null, false, true, false, true, false, false, null, cts);
            Assert.IsTrue(symres.Contains("sqldk!XeSosPkg::spinlock_backoff::Publish+425\r\nsqldk!SpinlockBase::Sleep+182\r\nsqlmin!Spinlock<143,7,1>::SpinToAcquireWithExponentialBackoff+363\r\nsqlmin!lck_lockInternal+2042\r\nsqlmin!MDL::LockGenericLocal+382\r\nsqlmin!MDL::LockGenericIdsLocal+101\r\nsqlmin!CMEDCacheEntryFactory::GetProxiedCacheEntryById+263\r\nsqlmin!CMEDProxyDatabase::GetOwnerByOwnerId+122\r\nsqllang!CSECAccessAuditBase::SetSecurable+427\r\nsqllang!CSECManager::_AccessCheck+151\r\nsqllang!CSECManager::AccessCheck+2346\r\nsqllang!FHasEntityPermissionsWithAuditState+1505\r\nsqllang!FHasEntityPermissions+165\r\nsqllang!CSQLObject::FPostCacheLookup+2562\r\nsqllang!CSQLSource::Transform+2194\r\nsqllang!CSQLSource::Execute+944\r\nsqllang!CStmtExecProc::XretLocalExec+622\r\nsqllang!CStmtExecProc::XretExecExecute+1153\r\nsqllang!CXStmtExecProc::XretExecute+56\r\nsqllang!CMsqlExecContext::ExecuteStmts<1,1>+1037\r\nsqllang!CMsqlExecContext::FExecute+2718\r\nsqllang!CSQLSource::Execute+2435\r\nsqllang!process_request+3681\r\nsqllang!process_commands_internal+735"));
        }

        /// Validate importing individual callstack events from XEL files.
        [TestMethod][TestCategory("Unit")] public async Task ImportIndividualXELEvents() {
            using var csr = new StackResolver();
            using var cts = new CancellationTokenSource();
            var ret = await csr.ExtractFromXELAsync(new[] { @"..\..\..\..\..\Tests\TestCases\ImportXEL\xe_wait_completed_0_132353446563350000.xel" }, false, new List<string>(new String[] { "callstack" }), cts);
            Assert.AreEqual(550, ret.Item1);
            Assert.IsTrue(ret.Item2.Contains("Tests\\TestCases\\ImportXEL\\xe_wait_completed_0_132353446563350000.xel, UTC: 2020-05-30 20:37:36.3626428, UUID: 992caa1d-ef90-4278-9821-ebdd0180db0d\"><action name='callstack'><value><![CDATA[0x00007FFAF2BD6C7C"));
            var res = await csr.ResolveCallstacksAsync(await csr.GetListofCallStacksAsync(ret.Item2, false, cts), string.Empty, false, null, false, false, false, false, false, false, null, cts);
            Assert.IsTrue(res.StartsWith(@"Event key: File: ..\..\..\..\..\Tests\TestCases\ImportXEL\xe_wait_completed_0_132353446563350000.xel, UTC: 2020-05-30 20:37:36.3626428, UUID: 992caa1d-ef90-4278-9821-ebdd0180db0d"));
        }

        [TestMethod][TestCategory("Unit")] public async Task XELActionsAndFieldsAsync() {
            using var csr = new StackResolver();
            using var cts = new CancellationTokenSource();
            var ret = await csr.GetDistinctXELFieldsAsync(new[] { @"..\..\..\..\..\Tests\TestCases\ImportXEL\xe_wait_completed_0_132353446563350000.xel" }, 1000, cts);
            Assert.AreEqual(1, ret.Item1.Count);   // just the callstack action
            Assert.AreEqual("callstack", ret.Item1.First());    // verify the name
            Assert.AreEqual(5, ret.Item2.Count);   // 5 fields
            Assert.AreEqual("duration", ret.Item2.First()); // first field in alphabetical order
            Assert.AreEqual("wait_type", ret.Item2.Last()); // last field in alphabetical order
        }

        [TestMethod][TestCategory("Unit")] public async Task XELActionsAndFieldsAsyncMultipleFiles() {
            using var csr = new StackResolver();
            using var cts = new CancellationTokenSource();
            var ret = await csr.GetDistinctXELFieldsAsync(new[] { @"..\..\..\..\..\Tests\TestCases\ImportXEL\xe_wait_completed_0_132353446563350000.xel", @"..\..\..\..\..\Tests\TestCases\ImportXEL\XESpins_0_131627061603030000.xel" }, 1000, cts);
            Assert.AreEqual(1, ret.Item1.Count);   // just the callstack action
            Assert.AreEqual("callstack", ret.Item1.First());    // verify the name
            Assert.AreEqual(9, ret.Item2.Count);   // 9 fields in total across the 2 XEL files
            Assert.AreEqual("backoffs", ret.Item2.First()); // first field across the 2 XEL files, in alphabetical order
            Assert.AreEqual("worker", ret.Item2.Last()); // last field across the 2 XEL files, in alphabetical order
        }

        /// Validate importing "single-line" callstack (such as when the input is copy-pasted from SSMS).
        [TestMethod][TestCategory("Unit")] public async Task SingleLineCallStack() {
            using var csr = new StackResolver();
            using var cts = new CancellationTokenSource();
            csr.ProcessBaseAddresses(File.ReadAllText(@"..\..\..\..\..\Tests\TestCases\ImportXEL\base_addresses.txt"));
            var pdbPath = @"..\..\..\..\..\Tests\TestCases\sqlsyms\13.0.4001.0\x64";
            var callStack = @"callstack	          0x00007FFEABD0D919        0x00007FFEABC4D45D      0x00007FFEAC0F7EE0  0x00007FFEAC0F80CF  0x00007FFEAC1EE447  0x00007FFEAC1EE6F5  0x00007FFEAC1D48B0  0x00007FFEAC71475A  0x00007FFEA9A708F1  0x00007FFEA9991FB9  0x00007FFEA9993D21  0x00007FFEA99B59F1  0x00007FFEA99B5055  0x00007FFEA99B2B8F  0x00007FFEA9675AD1  0x00007FFEA9671EFB  0x00007FFEAA37D83D  0x00007FFEAA37D241  0x00007FFEAA379F98  0x00007FFEA96719CA  0x00007FFEA9672933  0x00007FFEA9672041  0x00007FFEA967A82B  0x00007FFEA9681542  ";
            var symres = await csr.ResolveCallstacksAsync(await csr.GetListofCallStacksAsync(callStack, true, cts), pdbPath, false, null, false, false, false, true, false, false, null, cts);
            Assert.AreEqual("callstack\r\nsqldk!XeSosPkg::spinlock_backoff::Publish+425\r\nsqldk!SpinlockBase::Sleep+182\r\nsqlmin!Spinlock<143,7,1>::SpinToAcquireWithExponentialBackoff+363\r\nsqlmin!lck_lockInternal+2042\r\nsqlmin!MDL::LockGenericLocal+382\r\nsqlmin!MDL::LockGenericIdsLocal+101\r\nsqlmin!CMEDCacheEntryFactory::GetProxiedCacheEntryById+263\r\nsqlmin!CMEDProxyDatabase::GetOwnerByOwnerId+122\r\nsqllang!CSECAccessAuditBase::SetSecurable+427\r\nsqllang!CSECManager::_AccessCheck+151\r\nsqllang!CSECManager::AccessCheck+2346\r\nsqllang!FHasEntityPermissionsWithAuditState+1505\r\nsqllang!FHasEntityPermissions+165\r\nsqllang!CSQLObject::FPostCacheLookup+2562\r\nsqllang!CSQLSource::Transform+2194\r\nsqllang!CSQLSource::Execute+944\r\nsqllang!CStmtExecProc::XretLocalExec+622\r\nsqllang!CStmtExecProc::XretExecExecute+1153\r\nsqllang!CXStmtExecProc::XretExecute+56\r\nsqllang!CMsqlExecContext::ExecuteStmts<1,1>+1037\r\nsqllang!CMsqlExecContext::FExecute+2718\r\nsqllang!CSQLSource::Execute+2435\r\nsqllang!process_request+3681\r\nsqllang!process_commands_internal+735", symres.Trim());
        }

        /// Test for inline frame resolution. This test uses symbols for a Windows Driver Kit module, Wdf01000.sys, because private PDBs for that module are legitimately available on the
        /// Microsoft public symbols servers. https://github.com/microsoft/Windows-Driver-Frameworks/releases if interested.
        [TestMethod][TestCategory("Unit")] public async Task InlineFrameResolution() {
            using var csr = new StackResolver();
            using var cts = new CancellationTokenSource();
            var pdbPath = @"..\..\..\..\..\Tests\TestCases\SourceInformation";
            var ret = await csr.ResolveCallstacksAsync(await csr.GetListofCallStacksAsync("Wdf01000+17f27", false, cts), pdbPath, false, null, false, true, false, true, true, false, null, cts);
            Assert.AreEqual(
                "(Inline Function) Wdf01000!Mx::MxLeaveCriticalRegion+12	(minkernel\\wdf\\framework\\shared\\inc\\primitives\\km\\MxGeneralKm.h:198)\r\n(Inline Function) Wdf01000!FxWaitLockInternal::ReleaseLock+62	(minkernel\\wdf\\framework\\shared\\inc\\private\\common\\FxWaitLock.hpp:305)\r\n(Inline Function) Wdf01000!FxEnumerationInfo::ReleaseParentPowerStateLock+62	(minkernel\\wdf\\framework\\shared\\inc\\private\\common\\FxPkgPnp.hpp:510)\r\nWdf01000!FxPkgPnp::PowerPolicyCanChildPowerUp+143	(minkernel\\wdf\\framework\\shared\\inc\\private\\common\\FxPkgPnp.hpp:4127)",
                ret.Trim());
        }

        /// "Fuzz" test for inline frame resolution by providing random offsets into the module. This test uses symbols for a Windows Driver Kit module, Wdf01000.sys, because private PDBs for that module are legitimately available on the
        /// Microsoft public symbols servers. https://github.com/microsoft/Windows-Driver-Frameworks/releases if interested.
        [TestMethod][TestCategory("Unit")] public async Task RandomInlineFrameResolution() {
            using var csr = new StackResolver();
            using var cts = new CancellationTokenSource();
            var pdbPath = @"..\..\..\..\..\Tests\TestCases\SourceInformation";
            var callstackInput = new StringBuilder();
            // generate frames with random offsets
            var rng = new Random();
            // generate 1000 frames, each frame having a random offset into Wdf01000
            for (int frameNum = 0; frameNum < 1000; frameNum++) {
                callstackInput.AppendLine($"Wdf01000+{rng.Next(0, 1000000)}");
            }
            await csr.ResolveCallstacksAsync(await csr.GetListofCallStacksAsync(callstackInput.ToString(), false, cts), pdbPath, false, null, false, true, false, true, true, false, null, cts);
            // We do not need to check anything; the criteria for the test passing is that the call did not throw an unhandled exception
        }

        /// Test for inline frame resolution without source lines included This test uses symbols for a Windows Driver Kit module, Wdf01000.sys,
        /// because private PDBs for that module are legitimately available on the Microsoft public symbols servers. https://github.com/microsoft/Windows-Driver-Frameworks/releases if interested.
        [TestMethod][TestCategory("Unit")] public async Task InlineFrameResolutionNoSourceInfo() {
            using var csr = new StackResolver();
            using var cts = new CancellationTokenSource();
            var pdbPath = @"..\..\..\..\..\Tests\TestCases\SourceInformation";
            var ret = await csr.ResolveCallstacksAsync(await csr.GetListofCallStacksAsync("Wdf01000+17f27", false, cts), pdbPath, false, null, false, false, false, true, true, false, null, cts);
            Assert.AreEqual("(Inline Function) Wdf01000!Mx::MxLeaveCriticalRegion+12\r\n(Inline Function) Wdf01000!FxWaitLockInternal::ReleaseLock+62\r\n(Inline Function) Wdf01000!FxEnumerationInfo::ReleaseParentPowerStateLock+62\r\nWdf01000!FxPkgPnp::PowerPolicyCanChildPowerUp+143", ret.Trim());
        }

        [TestMethod][TestCategory("Unit")] public async Task InlineFrameResolutionNoSourceInfoWithInputFrameNums() {
            using var csr = new StackResolver();
            using var cts = new CancellationTokenSource();
            var pdbPath = @"..\..\..\..\..\Tests\TestCases\SourceInformation;..\..\..\..\..\Tests\TestCases\TestOrdinal";
            var ret = await csr.ResolveCallstacksAsync(await csr.GetListofCallStacksAsync("00 sqldk+0x40609\r\n01 Wdf01000+17f27\r\n02 sqldk+0x40609", false, cts), pdbPath, false, null, false, false, false, true, true, false, null, cts);
            Assert.AreEqual("00 sqldk!MemoryClerkInternal::AllocatePagesWithFailureMode+644\r\n01 (Inline Function) Wdf01000!Mx::MxLeaveCriticalRegion+12\r\n02 (Inline Function) Wdf01000!FxWaitLockInternal::ReleaseLock+62\r\n03 (Inline Function) Wdf01000!FxEnumerationInfo::ReleaseParentPowerStateLock+62\r\n04 Wdf01000!FxPkgPnp::PowerPolicyCanChildPowerUp+143\r\n05 sqldk!MemoryClerkInternal::AllocatePagesWithFailureMode+644", ret.Trim());
        }

        /// Tests the parsing and extraction of PDB details from a set of rows each with commma-separated fields. This sample mixes up \r\n and \n line-endings.
        [TestMethod][TestCategory("Unit")] public async Task ExtractModuleInfo() {
            using var cts = new CancellationTokenSource();
            var ret = await ModuleInfoHelper.ParseModuleInfoAsync(new List<StackDetails>() { new StackDetails("\"ntdll.dll\",\"10.0.19041.662\",2056192,666871280,2084960,\"ntdll.pdb\",\"{1EB9FACB-04C7-3C5D-EA71-60764CD333D0}\",0,1\r\n" +
"\"VCRUNTIME140.dll\",\"14.16.27033.0\",86016,1563486943,105788,\"vcruntime140.amd64.pdb\",\"{AF138C3F-2933-4097-8883-C1071B13375E}\",0,1\r\n\r\nsqlservr.exe,7ef4ea08-777a-43b7-8bce-4da6f0fa43c7,2\r\n\"KERNELBASE.dll\",\"10.0.19041.662\",2920448,3965251605,2936791,\"kernelbase.pdb\",\"{1FBE0B2B-89D1-37F0-1510-431FFFBA123E}\",0,1\n" +
"\"kernel32.dll\",\"10.0.19041.662\",774144,1262097423,770204,\"kernel32.pdb\",\"{54448D8E-EFC5-AB3C-7193-D2C7A6DF9008}\",0,1\r\n", false) }, cts);

            Assert.AreEqual(5, ret.Count());
            Assert.AreEqual("ntdll.pdb", ret["ntdll"].PDBName);
            Assert.AreEqual("1EB9FACB04C73C5DEA7160764CD333D0", ret["ntdll"].PDBGuid, ignoreCase: true);
            Assert.AreEqual(1, ret["ntdll"].PDBAge);
            Assert.AreEqual("vcruntime140.amd64.pdb", ret["VCRUNTIME140"].PDBName);
            Assert.AreEqual("AF138C3F293340978883C1071B13375E", ret["VCRUNTIME140"].PDBGuid, ignoreCase: true);
            Assert.AreEqual(1, ret["VCRUNTIME140"].PDBAge);
            Assert.AreEqual("sqlservr.pdb", ret["sqlservr"].PDBName);
            Assert.AreEqual("7ef4ea08777a43b78bce4da6f0fa43c7", ret["sqlservr"].PDBGuid, ignoreCase: true);
            Assert.AreEqual(2, ret["sqlservr"].PDBAge);
        }

        /// Tests the parsing and extraction of PDB details from a set of rows each with XML frames.
        [TestMethod][TestCategory("Unit")] public async Task ExtractModuleInfoXMLFrames() {
            var sample = new StackDetails("Frame = <frame id=\"00\" pdb=\"ntdll.pdb\" age=\"1\" guid=\"C374E059-5793-9B92-6525-386A66A2D3F5\" module=\"ntdll.dll\" rva=\"0x9F7E4\" />        \r\n" +
"Frame = <frame id=\"01\" pdb=\"kernelbase.pdb\" age=\"1\" guid=\"E77E26E7-D1C4-72BB-2C05-DD17624A9E58\" module=\"KERNELBASE.dll\" rva=\"0x38973\" />                          \n" +
"Frame = <frame id=\"02\" pdb=\"SqlDK.pdb\" age=\"2\" guid=\"6a193443-3512-464b-8b8e-\r\nd905ad930ee6\" module=\"sqldk.dll\" rva=\"0x40609\" />                                    \r\n" +
"<frame id=\"03\" pdb=\"vcruntime140.amd64.pdb\" age=\"1\" guid=\"AF138C3F-2933-4097-8883-C1071B13375E\" module=\"VCRUNTIME140.dll\" rva=\"0xB8F0\" />\r\n" +
"Frame = <frame id=\"04\" pdb=\"SqlDK.pdb\" age=\"2\" guid=\"6a193443-3512-464b-8b8e-d905ad930ee6\" module=\"sqldk.dll\" rva=\"0x2249f\" />                                    \n", false);
            using var cts = new CancellationTokenSource();
            var result = await ModuleInfoHelper.ParseModuleInfoXMLAsync(new List<StackDetails> { sample }, cts);

            var syms = result.Item1;
            Assert.AreEqual(4, syms.Count());
            Assert.AreEqual("ntdll.pdb", syms["ntdll"].PDBName);
            Assert.AreEqual("C374E05957939B926525386A66A2D3F5", syms["ntdll"].PDBGuid, ignoreCase: true);
            Assert.AreEqual(1, syms["ntdll"].PDBAge);
            Assert.AreEqual("kernelbase.pdb", syms["KERNELBASE"].PDBName);
            Assert.AreEqual("E77E26E7D1C472BB2C05DD17624A9E58", syms["KERNELBASE"].PDBGuid, ignoreCase: true);
            Assert.AreEqual(1, syms["KERNELBASE"].PDBAge);
            Assert.AreEqual("sqldk.pdb", syms["sqldk"].PDBName);
            Assert.AreEqual("6a1934433512464b8b8ed905ad930ee6", syms["sqldk"].PDBGuid, ignoreCase: true);
            Assert.AreEqual(2, syms["sqldk"].PDBAge);
            Assert.AreEqual("vcruntime140.amd64.pdb", syms["VCRUNTIME140"].PDBName);
            Assert.AreEqual("AF138C3F293340978883C1071B13375E", syms["VCRUNTIME140"].PDBGuid, ignoreCase: true);
            Assert.AreEqual(1, syms["VCRUNTIME140"].PDBAge);
        }

        /// Tests the parsing and extraction of PDB details from a set of rows each with XML frames. Some of those XML frames do not have sym info or RVA included.
        [TestMethod][TestCategory("Unit")] public async Task ExtractModuleInfoXMLFramesWithCalcBaseAddress() {
            using var cts = new CancellationTokenSource();
            var input = new StackDetails("Frame = <frame id=\"02\" pdb=\"SqlDK.pdb\" age=\"2\" guid=\"6a193443-3512-464b-8b8e-\r\nd905ad930ee6\" module=\"sqldk.dll\" rva=\"0x40609\" address = \"0x100440609\"/>\r\n<frame id=\"04\" name=\"sqldk.dll\" address=\"0x10042249f\" />\n", false);
            var result = await ModuleInfoHelper.ParseModuleInfoXMLAsync(new List<StackDetails> { input }, cts);

            var syms = result.Item1;
            Assert.AreEqual(1, syms.Count);
            Assert.AreEqual("sqldk.pdb", syms["sqldk"].PDBName);
            Assert.AreEqual("6a1934433512464b8b8ed905ad930ee6", syms["sqldk"].PDBGuid, ignoreCase: true);
            Assert.AreEqual(2, syms["sqldk"].PDBAge);
            Assert.AreEqual((ulong)0x100400000, syms["sqldk"].CalculatedModuleBaseAddress);
        }

        /// Tests the parsing and extraction of PDB details from a set of rows each with commma-separated fields.
        [TestMethod][TestCategory("Unit")] public async Task ExtractModuleInfoEmptyString() {
            using var cts = new CancellationTokenSource();
            var ret = await ModuleInfoHelper.ParseModuleInfoAsync(new List<StackDetails>() { new StackDetails(string.Empty, false) }, cts);
            Assert.AreEqual(ret.Count(), 0);
        }

        /// Test obtaining a local path for symbols downloaded from a symbol server.
        [TestMethod][TestCategory("Unit")] public async Task SymSrvLocalPaths() {
            using var csr = new StackResolver();
            using var cts = new CancellationTokenSource();
            var ret = await ModuleInfoHelper.ParseModuleInfoAsync(new List<StackDetails>() { new StackDetails("\"ntdll.dll\",\"10.0.19041.662\",2056192,666871280,2084960,\"ntdll.pdb\",\"{1EB9FACB-04C7-3C5D-EA71-60764CD333D0}\",0,1\r\n" +
"\"VCRUNTIME140.dll\",\"14.16.27033.0\",86016,1563486943,105788,\"vcruntime140.amd64.pdb\",\"{AF138C3F-2933-4097-8883-C1071B13375E}\",0,1\r\n\r\nsqlservr.exe,7ef4ea08-777a-43b7-8bce-4da6f0fa43c7,2\r\n" +
"\"KERNELBASE.dll\",\"10.0.19041.662\",2920448,3965251605,2936791,\"kernelbase.pdb\",\"{1FBE0B2B-89D1-37F0-1510-431FFFBA123E}\",0,1\n" +
"\"kernel32.dll\",\"10.0.19041.662\",774144,1262097423,770204,\"kernel32.pdb\",\"{54448D8E-EFC5-AB3C-7193-D2C7A6DF9008}\",0,1\r\n", false)}, cts);
            var paths = SymSrvHelpers.GetFolderPathsForPDBs(csr, "srv*https://msdl.microsoft.com/download/symbols", ret.Values.ToList());
            Assert.AreEqual(5, paths.Count);
        }

        /// End-to-end test with stacks being resolved based on symbols from symsrv.
        [TestMethod][TestCategory("Unit")] public async Task E2ESymSrv() {
            using var csr = new StackResolver();
            using var cts = new CancellationTokenSource();
            var pdbPath = @"srv*https://msdl.microsoft.com/download/symbols";
            var input = "ntdll+0x9F7E4\r\nKERNELBASE+0x38973\r\nVCRUNTIME140+0xB8F0\r\nntdll+0xA479F\r\nntdll+0x4BEF\r\nntdll+0x89E6\r\nKERNELBASE+0x396C9" +
"\r\n\"ntdll.dll\",\"10.0.17763.1490\",2019328,462107166,2009368,\"ntdll.pdb\",\"{C374E059-5793-9B92-6525-386A66A2D3F5}\",0,1\r\n" +
"\"KERNELBASE.dll\",\"10.0.17763.1518\",2707456,4281343292,2763414,\"kernelbase.pdb\",\"{E77E26E7-D1C4-72BB-2C05-DD17624A9E58}\",0,1\r\n" +
"\"VCRUNTIME140.dll\",\"14.16.27033.0\",86016,1563486943,105788,\"vcruntime140.amd64.pdb\",\"{AF138C3F-2933-4097-8883-C1071B13375E}\",0,1";

            var ret = await csr.ResolveCallstacksAsync(await csr.GetListofCallStacksAsync(input, false, cts), pdbPath, false, null, false, true, false, true, false, false, null, cts);
            var expected = "ntdll!NtWaitForSingleObject+20\r\nKERNELBASE!WaitForSingleObjectEx+147\r\nVCRUNTIME140!__C_specific_handler+160	(d:\\agent\\_work\\2\\s\\src\\vctools\\crt\\vcruntime\\src\\eh\\riscchandler.cpp:290)\r\nntdll!RtlpExecuteHandlerForException+15\r\nntdll!RtlDispatchException+1039\r\nntdll!RtlRaiseException+790\r\nKERNELBASE!RaiseException+105" +
"\r\n\"ntdll.dll\",\"10.0.17763.1490\",2019328,462107166,2009368,\"ntdll.pdb\",\"{C374E059-5793-9B92-6525-386A66A2D3F5}\",0,1\r\n" +
"\"KERNELBASE.dll\",\"10.0.17763.1518\",2707456,4281343292,2763414,\"kernelbase.pdb\",\"{E77E26E7-D1C4-72BB-2C05-DD17624A9E58}\",0,1\r\n" +
"\"VCRUNTIME140.dll\",\"14.16.27033.0\",86016,1563486943,105788,\"vcruntime140.amd64.pdb\",\"{AF138C3F-2933-4097-8883-C1071B13375E}\",0,1";
            Assert.AreEqual(expected.Trim(), ret.Trim());
        }

        /// End-to-end test with stacks being resolved based on symbols from symsrv, but just one line of input
        [TestMethod][TestCategory("Unit")] public async Task E2ESymSrvXMLSingleFrame() {
            using var csr = new StackResolver();
            using var cts = new CancellationTokenSource();
            var pdbPath = @"srv*https://msdl.microsoft.com/download/symbols";
            var input = "<frame id=\"00\" pdb=\"ntdll.pdb\" age=\"1\" guid=\"C374E059-5793-9B92-6525-386A66A2D3F5\" module=\"ntdll.dll\" rva=\"0x9F7E4\" />";
            var ret = await csr.ResolveCallstacksAsync(await csr.GetListofCallStacksAsync(input, false, cts), pdbPath, false, null, false, true, false, true, false, false, null, cts);
            var expected = @"00 ntdll!NtWaitForSingleObject+20";
            Assert.AreEqual(expected.Trim(), ret.Trim());
        }

        /// End-to-end test with stacks being resolved based on symbols from symsrv.
        [TestMethod][TestCategory("Unit")] public async Task E2ESymSrvXMLFrames() {
            using var csr = new StackResolver();
            using var cts = new CancellationTokenSource();
            var pdbPath = @"srv*https://msdl.microsoft.com/download/symbols";
            var input = "Frame = <frame id=\"00\" pdb=\"ntdll.pdb\" age=\"1\" guid=\"C374E059-5793-9B92-6525-386A66A2D3F5\" module=\"ntdll.dll\" rva=\"0x9F7E4\" />        \r\n" +
"Frame = <frame id=\"01\" pdb=\"kernelbase.pdb\" age=\"1\" guid=\"E77E26E7-D1C4-72BB-2C05-DD17624A9E58\" module=\"KERNELBASE.dll\" rva=\"0x38973\" />                          \n" +
"Frame = <frame id=\"02\" pdb=\"SqlDK.pdb\" age=\"2\" guid=\"6a193443-3512-464b-8b8e-\r\nd905ad930ee6\" module=\"sqldk.dll\" rva=\"0x40609\" />                                    \r\n" +
"<frame id=\"03\" pdb=\"vcruntime140.amd64.pdb\" age=\"1\" guid=\"AF138C3F-2933-4097-8883-C1071B13375E\" module=\"VCRUNTIME140.dll\" rva=\"0xB8F0\" />\r\n" +
"Frame = <frame id=\"04\" pdb=\"SqlDK.pdb\" age=\"2\" guid=\"6a193443-3512-464b-8b8e-d905ad930ee6\" module=\"sqldk.dll\" rva=\"0x2249f\" />                                    \n";

            var ret = await csr.ResolveCallstacksAsync(await csr.GetListofCallStacksAsync(input, false, cts), pdbPath, false, null, false, true, false, true, false, false, null, cts);
            var expected = "00 ntdll!NtWaitForSingleObject+20\r\n01 KERNELBASE!WaitForSingleObjectEx+147\r\n02 sqldk!MemoryClerkInternal::AllocatePagesWithFailureMode+644\r\n03 VCRUNTIME140!__C_specific_handler+160	(d:\\agent\\_work\\2\\s\\src\\vctools\\crt\\vcruntime\\src\\eh\\riscchandler.cpp:290)\r\n04 sqldk!Spinlock<244,2,1>::SpinToAcquireWithExponentialBackoff+349";
            Assert.AreEqual(expected.Trim(), ret.Trim());
        }

        [TestMethod][TestCategory("Unit")] public async Task E2ESymSrvXMLFramesModuleFileExtensionMissing() {
            using var csr = new StackResolver();
            using var cts = new CancellationTokenSource();
            var pdbPath = @"srv*https://msdl.microsoft.com/download/symbols";
            var input = "Frame = <frame id=\"00\" pdb=\"ntdll.pdb\" age=\"1\" guid=\"C374E059-5793-9B92-6525-386A66A2D3F5\" module=\"ntdll\" rva=\"0x9F7E4\" />        \r\n" +
"Frame = <frame id=\"01\" pdb=\"kernelbase.pdb\" age=\"1\" guid=\"E77E26E7-D1C4-72BB-2C05-DD17624A9E58\" module=\"KERNELBASE\" rva=\"0x38973\" />                          \n" +
"Frame = <frame id=\"02\" pdb=\"SqlDK.pdb\" age=\"2\" guid=\"6a193443-3512-464b-8b8e-\r\nd905ad930ee6\" module=\"sqldk\" rva=\"0x40609\" />                                    \r\n" +
"<frame id=\"03\" pdb=\"vcruntime140.amd64.pdb\" age=\"1\" guid=\"AF138C3F-2933-4097-8883-C1071B13375E\" module=\"VCRUNTIME140\" rva=\"0xB8F0\" />\r\n" +
"Frame = <frame id=\"04\" pdb=\"SqlDK.pdb\" age=\"2\" guid=\"6a193443-3512-464b-8b8e-d905ad930ee6\" module=\"sqldk\" rva=\"0x2249f\" />                                    \n";

            var ret = await csr.ResolveCallstacksAsync(await csr.GetListofCallStacksAsync(input, false, cts), pdbPath, false, null, false, true, false, true, false, false, null, cts);
            var expected = "00 ntdll!NtWaitForSingleObject+20\r\n01 KERNELBASE!WaitForSingleObjectEx+147\r\n02 sqldk!MemoryClerkInternal::AllocatePagesWithFailureMode+644\r\n03 VCRUNTIME140!__C_specific_handler+160	(d:\\agent\\_work\\2\\s\\src\\vctools\\crt\\vcruntime\\src\\eh\\riscchandler.cpp:290)\r\n04 sqldk!Spinlock<244,2,1>::SpinToAcquireWithExponentialBackoff+349";
            Assert.AreEqual(expected.Trim(), ret.Trim());
        }

        /// End-to-end test with stacks being resolved based on symbols from symsrv, with a frame that needs "calculated base address" handling
        [TestMethod][TestCategory("Unit")] public async Task E2ESymSrvXMLFramesWithCalcBaseAddress() {
            var input = "Frame = <frame id=\"02\" pdb=\"SqlDK.pdb\" age=\"2\" guid=\"6a193443-3512-464b-8b8e-\r\nd905ad930ee6\" module=\"sqldk.dll\" rva=\"0x40609\" address = \"0x100440609\"/>\r\n<frame id=\"03\" name=\"sqldk.dll\" address=\"0x10042249f\" />\n";
            using var csr = new StackResolver();
            using var cts = new CancellationTokenSource();
            var pdbPath = @"srv*https://msdl.microsoft.com/download/symbols";
            var ret = await csr.ResolveCallstacksAsync(await csr.GetListofCallStacksAsync(input, false, cts), pdbPath, false, null, false, true, false, true, false, false, null, cts);
            var expected = "02 sqldk!MemoryClerkInternal::AllocatePagesWithFailureMode+644\r\n03 sqldk!Spinlock<244,2,1>::SpinToAcquireWithExponentialBackoff+349";
            Assert.AreEqual(expected.Trim(), ret.Trim());
        }

        /// End-to-end test with XE histogram target and XML frames.
        [TestMethod][TestCategory("Unit")] public async Task E2ESymSrvXMLFramesHistogram() {
            using var csr = new StackResolver();
            using var cts = new CancellationTokenSource();
            var pdbPath = @"srv*https://msdl.microsoft.com/download/symbols";
            var input = "<HistogramTarget truncated=\"0\" buckets=\"256\"><Slot count=\"5\"><value><![CDATA[<frame id=\"00\" pdb=\"ntdll.pdb\" age=\"1\" guid=\"C374E059-5793-9B92-6525-386A66A2D3F5\" module=\"ntdll.dll\" rva=\"0x9F7E4\" />" +
"<frame id=\"01\" pdb=\"kernelbase.pdb\" age=\"1\" guid=\"E77E26E7-D1C4-72BB-2C05-DD17624A9E58\" module=\"KERNELBASE.dll\" rva=\"0x38973\" />" +
"<frame id=\"02\" pdb=\"SqlDK.pdb\" age=\"2\" guid=\"6a193443-3512-464b-8b8e-d905ad930ee6\" module=\"sqldk.dll\" rva=\"0x40609\" />" +
"]]></value></Slot><Slot count=\"3\"><value><![CDATA[<frame id=\"00\" pdb=\"vcruntime140.amd64.pdb\" age=\"1\" guid=\"AF138C3F-2933-4097-8883-C1071B13375E\" module=\"VCRUNTIME140.dll\" rva=\"0xB8F0\" />" +
"<frame id=\"01\" pdb=\"SqlDK.pdb\" age=\"2\" guid=\"6a193443-3512-464b-8b8e-d905ad930ee6\" module=\"sqldk.dll\" rva=\"0x2249f\" />" +
"]]></value></Slot></HistogramTarget>";

            var ret = await csr.ResolveCallstacksAsync(await csr.GetListofCallStacksAsync(input, false, cts), pdbPath, false, null, false, true, false, true, false, false, null, cts);
            var expected = "Slot_0	[count:5]:\r\n\r\n00 ntdll!NtWaitForSingleObject+20\r\n01 KERNELBASE!WaitForSingleObjectEx+147\r\n02 sqldk!MemoryClerkInternal::AllocatePagesWithFailureMode+644\r\n\r\nSlot_1	[count:3]:\r\n\r\n00 VCRUNTIME140!__C_specific_handler+160	(d:\\agent\\_work\\2\\s\\src\\vctools\\crt\\vcruntime\\src\\eh\\riscchandler.cpp:290)\r\n01 sqldk!Spinlock<244,2,1>::SpinToAcquireWithExponentialBackoff+349";
            Assert.AreEqual(expected.Trim(), ret.Trim());
        }

        /// End-to-end test with XE histogram target and module+offset frames.
        [TestMethod][TestCategory("Unit")] public async Task E2EHistogramAddresses() {
            using var csr = new StackResolver();
            using var cts = new CancellationTokenSource();
            csr.ProcessBaseAddresses(File.ReadAllText(@"..\..\..\..\..\Tests\TestCases\ImportXEL\base_addresses.txt"));
            var pdbPath = @"..\..\..\..\..\Tests\TestCases\sqlsyms\13.0.4001.0\x64";
            var input = "<HistogramTarget truncated=\"0\" buckets=\"256\"><Slot count=\"5\"><value>0x00007FFEABD0D919        0x00007FFEABC4D45D      0x00007FFEAC0F7EE0  0x00007FFEAC0F80CF  </value></Slot></HistogramTarget>";
            var ret = await csr.ResolveCallstacksAsync(await csr.GetListofCallStacksAsync(input, true, cts), pdbPath, false, null, true, false, false, true, false, false, null, cts);
            var expected = "Slot_0	[count:5]:\r\n\r\nsqldk!XeSosPkg::spinlock_backoff::Publish+425\r\nsqldk!SpinlockBase::Sleep+182\r\nsqlmin!Spinlock<143,7,1>::SpinToAcquireWithExponentialBackoff+363\r\nsqlmin!lck_lockInternal+2042";
            Assert.AreEqual(expected.Trim(), ret.Trim());
        }

        [TestMethod][TestCategory("Unit")] public async Task E2ESymSrvXMLFramesMultiHistogram() {
            using var csr = new StackResolver();
            using var cts = new CancellationTokenSource();
            var pdbPath = @"srv*https://msdl.microsoft.com/download/symbols";
            var input = "Annotation for histogram \r\n#1\r\n<HistogramTarget truncated=\"0\" buckets=\"256\"><Slot count=\"5\"><value><![CDATA[<frame id=\"00\" pdb=\"ntdll.pdb\" age=\"1\" guid=\"C374E059-5793-9B92-6525-386A66A2D3F5\" module=\"ntdll.dll\" rva=\"0x9F7E4\" />" +
"<frame id=\"01\" pdb=\"kernelbase.pdb\" age=\"1\" guid=\"E77E26E7-D1C4-72BB-2C05-DD17624A9E58\" module=\"KERNELBASE.dll\" rva=\"0x38973\" />" +
"<frame id=\"02\" pdb=\"SqlDK.pdb\" age=\"2\" guid=\"6a193443-3512-464b-8b8e-d905ad930ee6\" module=\"sqldk.dll\" rva=\"0x40609\" />" +
"]]></value></Slot></HistogramTarget>\r\nAnnotation for histogram \r\n#2\r\n<HistogramTarget truncated=\"0\" buckets=\"256\"><Slot count=\"5\"><value><![CDATA[<frame id=\"00\" pdb=\"ntdll.pdb\" age=\"1\" guid=\"C374E059-5793-9B92-6525-386A66A2D3F5\" module=\"ntdll.dll\" rva=\"0x9F7E4\" />" +
"<frame id=\"01\" pdb=\"kernelbase.pdb\" age=\"1\" guid=\"E77E26E7-D1C4-72BB-2C05-DD17624A9E58\" module=\"KERNELBASE.dll\" rva=\"0x38973\" />" +
"<frame id=\"02\" pdb=\"SqlDK.pdb\" age=\"2\" guid=\"6a193443-3512-464b-8b8e-d905ad930ee6\" module=\"sqldk.dll\" rva=\"0x40609\" />" +
"]]></value></Slot><Slot count=\"3\"><value><![CDATA[<frame id=\"00\" pdb=\"vcruntime140.amd64.pdb\" age=\"1\" guid=\"AF138C3F-2933-4097-8883-C1071B13375E\" module=\"VCRUNTIME140.dll\" rva=\"0xB8F0\" />" +
"<frame id=\"01\" pdb=\"SqlDK.pdb\" age=\"2\" guid=\"6a193443-3512-464b-8b8e-d905ad930ee6\" module=\"sqldk.dll\" rva=\"0x2249f\" />]]></value></Slot></HistogramTarget>";

            var ret = await csr.ResolveCallstacksAsync(await csr.GetListofCallStacksAsync(input, false, cts), pdbPath, false, null, false, true, false, true, false, false, null, cts);
            var expected = "Annotation for histogram #1\r\nSlot_0	[count:5]:\r\n\r\n00 ntdll!NtWaitForSingleObject+20\r\n01 KERNELBASE!WaitForSingleObjectEx+147\r\n02 sqldk!MemoryClerkInternal::AllocatePagesWithFailureMode+644\r\n\r\nAnnotation for histogram #2\r\nSlot_1	[count:5]:\r\n\r\n00 ntdll!NtWaitForSingleObject+20\r\n01 KERNELBASE!WaitForSingleObjectEx+147\r\n02 sqldk!MemoryClerkInternal::AllocatePagesWithFailureMode+644\r\n\r\nAnnotation for histogram #2\r\nSlot_2	[count:3]:\r\n\r\n00 VCRUNTIME140!__C_specific_handler+160	(d:\\agent\\_work\\2\\s\\src\\vctools\\crt\\vcruntime\\src\\eh\\riscchandler.cpp:290)\r\n01 sqldk!Spinlock<244,2,1>::SpinToAcquireWithExponentialBackoff+349";
            Assert.AreEqual(expected.Trim(), ret.Trim());
        }

        [TestMethod][TestCategory("Unit")] public async Task E2ESymSrvXMLFramesMultiHistogramAddressesSingleLine() {
            using var csr = new StackResolver();
            using var cts = new CancellationTokenSource();
            csr.ProcessBaseAddresses(File.ReadAllText(@"..\..\..\..\..\Tests\TestCases\ImportXEL\base_addresses.txt"));
            var pdbPath = @"..\..\..\..\..\Tests\TestCases\sqlsyms\13.0.4001.0\x64";
            var input = "Annotation for histogram #1    <HistogramTarget truncated=\"0\" buckets=\"256\"><Slot count=\"5\"><value>0x00007FFEABD0D919        0x00007FFEABC4D45D      0x00007FFEAC0F7EE0  0x00007FFEAC0F80CF</value></Slot></HistogramTarget>" +
                "Annotation for histogram #2    <HistogramTarget truncated=\"0\" buckets=\"256\"><Slot count=\"5\"><value>0x00007FFEAC0F80CF  0x00007FFEAC1EE447  0x00007FFEAC1EE6F5</value></Slot></HistogramTarget>";

            var ret = await csr.ResolveCallstacksAsync(await csr.GetListofCallStacksAsync(input, true, cts), pdbPath, false, null, false, false, false, true, false, false, null, cts);
            var expected = "Annotation for histogram #1\r\nSlot_0	[count:5]:\r\n\r\nsqldk!XeSosPkg::spinlock_backoff::Publish+425\r\nsqldk!SpinlockBase::Sleep+182\r\nsqlmin!Spinlock<143,7,1>::SpinToAcquireWithExponentialBackoff+363\r\nsqlmin!lck_lockInternal+2042\r\nAnnotation for histogram #2\r\nSlot_1	[count:5]:\r\n\r\nsqlmin!lck_lockInternal+2042\r\nsqlmin!MDL::LockGenericLocal+382\r\nsqlmin!MDL::LockGenericIdsLocal+101";
            Assert.AreEqual(expected.Trim(), ret.Trim());
        }

        [TestMethod][TestCategory("Unit")] public async Task E2ESymSrvXMLFramesMultiHistogramAddressesSingleLineTrailingText() {
            using var csr = new StackResolver();
            using var cts = new CancellationTokenSource();
            csr.ProcessBaseAddresses(File.ReadAllText(@"..\..\..\..\..\Tests\TestCases\ImportXEL\base_addresses.txt"));
            var pdbPath = @"..\..\..\..\..\Tests\TestCases\sqlsyms\13.0.4001.0\x64";
            var input = "Annotation for histogram #1    <HistogramTarget truncated=\"0\" buckets=\"256\"><Slot count=\"5\"><value>0x00007FFEABD0D919        0x00007FFEABC4D45D      </value></Slot></HistogramTarget> trailing text 1" +
                "Annotation for histogram #2    <HistogramTarget truncated=\"0\" buckets=\"256\"><Slot count=\"5\"><value>0x00007FFEAC1EE447  0x00007FFEAC1EE6F5</value></Slot></HistogramTarget>     trailing text 2";
            var ret = await csr.ResolveCallstacksAsync(await csr.GetListofCallStacksAsync(input, true, cts), pdbPath, false, null, false, false, false, true, false, false, null, cts);
            var expected = "Annotation for histogram #1\r\nSlot_0	[count:5]:\r\n\r\nsqldk!XeSosPkg::spinlock_backoff::Publish+425\r\nsqldk!SpinlockBase::Sleep+182\r\n\r\ntrailing text 1Annotation for histogram #2trailing text 2\r\nSlot_1	[count:5]:\r\n\r\nsqlmin!MDL::LockGenericLocal+382\r\nsqlmin!MDL::LockGenericIdsLocal+101";
            Assert.AreEqual(expected.Trim(), ret.Trim());
        }

        [TestMethod][TestCategory("Unit")] public async Task E2EHistogramAddressesFuzz() {
            using var csr = new StackResolver();
            using var cts = new CancellationTokenSource();
            csr.ProcessBaseAddresses(File.ReadAllText(@"..\..\..\..\..\Tests\TestCases\ImportXEL\base_addresses.txt"));
            var pdbPath = @"..\..\..\..\..\Tests\TestCases\sqlsyms\13.0.4001.0\x64";
            var input = "<HistogramTargetWrongTag truncated=\"0\" buckets=\"256\"><Slot count=\"5\"><value>0x00007FFEABD0D919        0x00007FFEABC4D45D      0x00007FFEAC0F7EE0  0x00007FFEAC0F80CF  </value></Slot></HistogramTargetWrongTag>";
            var ret = await csr.ResolveCallstacksAsync(await csr.GetListofCallStacksAsync(input, true, cts), pdbPath, false, null, false, false, false, true, false, false, null, cts);
            var expected = "<HistogramTargetWrongTag\r\ntruncated=\"0\"\r\nbuckets=\"256\"><Slot\r\ncount=\"5\"><value>0x00007FFEABD0D919\r\nsqldk!SpinlockBase::Sleep+182\r\nsqlmin!Spinlock<143,7,1>::SpinToAcquireWithExponentialBackoff+363\r\nsqlmin!lck_lockInternal+2042\r\n</value></Slot></HistogramTargetWrongTag>";
            Assert.AreEqual(expected.Trim(), ret.Trim()); // we just expect the input text back as-is
            input = "<HistogramTarget truncated=\"0\" buckets=\"256\"><Slot count=\"5\"><value>0x00007FFEABD0D919        0x00007FFEABC4D45D      0x00007FFEAC0F7EE0  0x00007FFEAC0F80CF  </value></Slot></HistogramTargetWrongTag>";
            ret = await csr.ResolveCallstacksAsync(await csr.GetListofCallStacksAsync(input, true, cts), pdbPath, false, null, false, false, false, true, false, false, null, cts);
            expected = "<HistogramTarget\r\ntruncated=\"0\"\r\nbuckets=\"256\"><Slot\r\ncount=\"5\"><value>0x00007FFEABD0D919\r\nsqldk!SpinlockBase::Sleep+182\r\nsqlmin!Spinlock<143,7,1>::SpinToAcquireWithExponentialBackoff+363\r\nsqlmin!lck_lockInternal+2042\r\n</value></Slot></HistogramTargetWrongTag>";
            Assert.AreEqual(expected.Trim(), ret.Trim()); // we just expect the input text back as-is
        }

        /// End-to-end test with stacks being resolved based on symbols from symsrv.
        [TestMethod][TestCategory("Unit")] public async Task E2ESymSrvXMLFramesMixedLineEndings() {
            using var csr = new StackResolver();
            using var cts = new CancellationTokenSource();
            var pdbPath = @"srv*https://msdl.microsoft.com/download/symbols";
            var input = "<frame id=\"00\" pdb=\"ntdll.pdb\" age=\"1\" guid=\"C374E059-5793-9B92-6525-386A66A2D3F5\" module=\"ntdll.dll\" rva=\"0x9F7E4\" />" +
"<frame id=\"01\" pdb=\"kernelbase.pdb\" age=\"1\" guid=\"E77E26E7-D1C4-72BB-2C05-DD17624A9E58\" module=\"KERNELBASE.dll\" rva=\"0x38973\" />" +
"<frame id=\"02\" pdb=\"SqlDK.pdb\" age=\"2\" guid=\"6a193443-3512-464b-8b8e-\r\nd905ad930ee6\" module=\"sqldk.dll\" rva=\"0x40609\" />                                    \r\n" +
"<frame id=\"03\" pdb=\"vcruntime140.amd64.pdb\" age=\"1\" guid=\"AF138C3F-2933-4097-8883-C1071B13375E\" module=\"VCRUNTIME140.dll\" rva=\"0xB8F0\" />" +
"<frame id=\"04\" pdb=\"SqlDK.pdb\" age=\"2\" guid=\"6a193443-3512-464b-8b8e-d905ad930ee6\" module=\"sqldk.dll\" rva=\"0x2249f\" />                                    \n";

            var ret = await csr.ResolveCallstacksAsync(await csr.GetListofCallStacksAsync(input, false, cts), pdbPath, false, null, false, true, false, true, false, false, null, cts);
            var expected = "00 ntdll!NtWaitForSingleObject+20\r\n01 KERNELBASE!WaitForSingleObjectEx+147\r\n02 sqldk!MemoryClerkInternal::AllocatePagesWithFailureMode+644\r\n03 VCRUNTIME140!__C_specific_handler+160	(d:\\agent\\_work\\2\\s\\src\\vctools\\crt\\vcruntime\\src\\eh\\riscchandler.cpp:290)\r\n04 sqldk!Spinlock<244,2,1>::SpinToAcquireWithExponentialBackoff+349\r\n";
            Assert.AreEqual(expected.Trim(), ret.Trim());
        }

        /// End-to-end test with stacks being resolved based on symbols from symsrv.
        [TestMethod][TestCategory("Unit")] public async Task E2ESymSrvNoSympath() {
            using var csr = new StackResolver();
            using var cts = new CancellationTokenSource();
            var pdbPath = string.Empty;
            var input = @"ntdll+0x9F7E4\r\nKERNELBASE+0x38973\r\nVCRUNTIME140+0xB8F0\r\nntdll+0xA479F\r\nntdll+0x4BEF\r\nntdll+0x89E6\r\nKERNELBASE+0x396C9\r\n" +
"\"ntdll.dll\",\"10.0.17763.1490\",2019328,462107166,2009368,\"ntdll.pdb\",\"{C374E059-5793-9B92-6525-386A66A2D3F5}\",0,1\r\n" +
"\"KERNELBASE.dll\",\"10.0.17763.1518\",2707456,4281343292,2763414,\"kernelbase.pdb\",\"{E77E26E7-D1C4-72BB-2C05-DD17624A9E58}\",0,1\r\n" +
"\"VCRUNTIME140.dll\",\"14.16.27033.0\",86016,1563486943,105788,\"vcruntime140.amd64.pdb\",\"{AF138C3F-2933-4097-8883-C1071B13375E}\",0,1\r\n";

            var ret = await csr.ResolveCallstacksAsync(await csr.GetListofCallStacksAsync(input, false, cts), pdbPath, false, null, false, true, false, true, false, false, null, cts);
            Assert.AreEqual(input.Trim(), ret.Trim());
        }

        /// Test for exported symbols
        [TestMethod][TestCategory("Unit")] public void ExportedSymbols() {
            var ret = ExportedSymbol.GetExports(@"..\..\..\..\..\Tests\TestCases\TestOrdinal\sqldk.dll");
            Assert.AreEqual(931, ret.Count);
            Assert.AreEqual((uint)1095072, ret[15].Address);
            Assert.AreEqual((uint)897568, ret[259].Address);
            Assert.AreEqual((uint)58752, ret[684].Address);
            Assert.AreEqual((uint)1447120, ret[1161].Address);
        }

        [TestMethod][TestCategory("Unit")] public async Task IsUrlValid() {
            Assert.IsFalse(await Symbol.IsURLValid(new Uri("https://msdl.microsoft.com/download/symbols/sqldk.pdb/6a1934433512464b8b8ed905ad930ee62/someother.pdb")));
            Assert.IsFalse(await Symbol.IsURLValid(new Uri("file:///C:/Windows/System32/Kernel32.dll")));
            Assert.IsFalse(await Symbol.IsURLValid(new Uri("LDAP://server/distinguishedName")));
            Assert.IsTrue(await Symbol.IsURLValid(new Uri("https://msdl.microsoft.com/download/symbols/sqldk.pdb/6a1934433512464b8b8ed905ad930ee62/sqldk.pdb")));
        }

        [TestMethod][TestCategory("Unit")] public void VerifyBuildInfo() {
            var builds = SQLBuildInfo.GetSqlBuildInfo(@"..\..\..\..\..\Tests\TestCases\buildinfo.sample.json");
            Assert.AreEqual(2, builds.Count);
            Assert.AreEqual(builds["SQL Server 2019 RTM RTM - 15.0.2000.5 - x64 (Nov 2019)"].SymbolDetails[0].PDBName, "SqlDK");
            var outputFilename = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            builds["SQL Server 2019 RTM RTM - 15.0.2000.5 - x64 (Nov 2019)"].SymbolDetails[0].DownloadURL = "https://localhost/fake.pdb";
            SQLBuildInfo.SaveSqlBuildInfo(builds.Values.ToList(), outputFilename);
            builds = SQLBuildInfo.GetSqlBuildInfo(outputFilename); // read back
            Assert.AreEqual(2, builds.Count);
            Assert.AreEqual("https://localhost/fake.pdb", builds["SQL Server 2019 RTM RTM - 15.0.2000.5 - x64 (Nov 2019)"].SymbolDetails[0].DownloadURL);
        }

        /// Test cancellation of various operations
        [TestMethod][TestCategory("Unit")] public async Task Cancellations() {
            using var csr = new StackResolver();
            using var cts = new CancellationTokenSource();
            var xelTask = csr.ExtractFromXELAsync(new[] { @"..\..\..\..\..\Tests\TestCases\ImportXEL\XESpins_0_131627061603030000.xel" }, true, new List<string>(new string[] { "callstack" }), cts);
            while (true) {
                if (xelTask.Wait(StackResolver.OperationWaitIntervalMilliseconds)) break;
                cts.Cancel();
            }
            Assert.AreEqual(0, xelTask.Result.Item1);
            Assert.AreEqual(StackResolver.OperationCanceled, xelTask.Result.Item2);

            using var cts2 = new CancellationTokenSource();
            var xelFieldsTask = csr.GetDistinctXELFieldsAsync(new[] { @"..\..\..\..\..\Tests\TestCases\ImportXEL\XESpins_0_131627061603030000.xel" }, int.MaxValue, cts2);
            while (true) {
                if (xelFieldsTask.Wait(StackResolver.OperationWaitIntervalMilliseconds)) break;
                cts2.Cancel();
            }
            Assert.AreEqual(0, xelFieldsTask.Result.Item1.Count);
            Assert.AreEqual(0, xelFieldsTask.Result.Item2.Count);

            using var cts3 = new CancellationTokenSource();
            Assert.IsTrue(csr.ProcessBaseAddresses(@"c:\mssql\binn\sqldk.dll 00000001`00400000"));
            var xeventInput = PrepareLargeXEventInput().ToString();
            var xeStacks = await csr.GetListofCallStacksAsync(xeventInput, false, cts3);
            var resolveStacksTask = csr.ResolveCallstacksAsync(xeStacks, @"..\..\..\..\..\Tests\TestCases\TestOrdinal", false, null, false, false, false, true, false, false, Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()), cts3);
            while (true) {
                if (resolveStacksTask.Wait(StackResolver.OperationWaitIntervalMilliseconds)) break;
                cts3.Cancel();
            }
            Assert.AreEqual(StackResolver.OperationCanceled, resolveStacksTask.Result);

            using var cts4 = new CancellationTokenSource();
            var parseModuleInfoXMLTask = ModuleInfoHelper.ParseModuleInfoXMLAsync(xeStacks, cts4);
            while (true) {
                cts4.Cancel();  // because this method is quick, we need to simulate a cancel right away
                if (parseModuleInfoXMLTask.Wait(StackResolver.OperationWaitIntervalMilliseconds)) break;
            }
            Assert.AreEqual(0, parseModuleInfoXMLTask.Result.Item1.Count);
            Assert.AreEqual(0, parseModuleInfoXMLTask.Result.Item2.Count);

            using var cts5 = new CancellationTokenSource();
            var parseModuleInfoTask = ModuleInfoHelper.ParseModuleInfoAsync(xeStacks, cts5);
            while (true) {
                if (parseModuleInfoTask.Wait(StackResolver.OperationWaitIntervalMilliseconds)) break;
                cts5.Cancel();
            }
            Assert.AreEqual(0, parseModuleInfoTask.Result.Count);
            Assert.AreEqual(0, parseModuleInfoTask.Result.Count);
        }
    }
}
