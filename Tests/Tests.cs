// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver {
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using Xunit;

    /// Class implementing xUnit tests.
    public class Tests {
        /// Validate that "block symbols" in a PDB are resolved correctly.
        [Fact]
        public void BlockResolution() {
            using (var csr = new StackResolver()) {
                var pdbPath = @"..\..\..\Tests\TestCases\TestBlockResolution";
                var ret = csr.ResolveCallstacks("Return Addr: 00007FF830D4CDA4 Module(KERNELBASE+000000000009CDA4)", pdbPath, false, null, false, false, false, false, true, false, false, null);
                Assert.Equal("KERNELBASE!SignalObjectAndWait+147716", ret.Trim());
            }
        }

        /// Test the resolution of OrdinalNNN symbols to their actual names.
        [Fact]
        public void OrdinalBasedSymbol() {
            using (var csr = new StackResolver()) {
                var dllPaths = new List<string>{@"..\..\..\Tests\TestCases\TestOrdinal"};
                var ret = csr.ResolveCallstacks("sqldk!Ordinal298+00000000000004A5", @"..\..\..\Tests\TestCases\TestOrdinal", false, dllPaths, false, false, false, false, true, false, false, null);
                Assert.Equal("sqldk!SOS_Scheduler::SwitchContext+941", ret.Trim());
            }
        }

        /// Test the resolution of a "regular" symbol with input specifying a hex offset into module.
        [Fact]
        public void RegularSymbolHexOffset() {
            using (var csr = new StackResolver()) {
                var ret = csr.ResolveCallstacks("sqldk+0x40609\r\nsqldk+40609", @"..\..\..\Tests\TestCases\TestOrdinal", false, null, false, false, false, false, true, false, false, null);
                var expectedSymbol = "sqldk!MemoryClerkInternal::AllocatePagesWithFailureMode+644";
                Assert.Equal(expectedSymbol + Environment.NewLine + expectedSymbol, ret.Trim());
            }
        }

        /// Test the resolution of a "regular" symbol with virtual address as input.
        [Fact]
        public void RegularSymbolVirtualAddress() {
            using (var csr = new StackResolver()) {
                var moduleAddressesGood = @"c:\mssql\binn\sqldk.dll 00000001`00400000";
                Assert.True(csr.ProcessBaseAddresses(moduleAddressesGood));
                var ret = csr.ResolveCallstacks("0x000000010042249f", @"..\..\..\Tests\TestCases\TestOrdinal", false, null, false, false, false, false, true, false, false, null);
                var expectedSymbol = "sqldk!Spinlock<244,2,1>::SpinToAcquireWithExponentialBackoff+349";
                Assert.Equal(expectedSymbol, ret.Trim());
            }
        }

        /// Check the processing of module base address information.
        [Fact]
        public void ModuleLoadAddressInputEmptyString() {
            using (var csr = new StackResolver()) {
                Assert.True(csr.ProcessBaseAddresses(string.Empty));
            }
        }

        /// Check the processing of module base address information.
        [Fact]
        public void ModuleLoadAddressInputJunkString() {
            using (var csr = new StackResolver()) {
                var moduleAddressesBad = @"hello wor1213ld";
                Assert.False(csr.ProcessBaseAddresses(moduleAddressesBad));
            }
        }

        /// Check the processing of module base address information.
        [Fact]
        public void ModuleLoadAddressInputColHeaders() {
            using (var csr = new StackResolver()) {
                var moduleAddressesColHeader = File.ReadAllText(@"..\..\..\Tests\TestCases\ImportXEL\xe_wait_base_addresses.txt");
                var loadstatus = csr.ProcessBaseAddresses(moduleAddressesColHeader);
                Assert.True(loadstatus);
                Assert.Equal(26, csr.LoadedModules.Count);

                var sqllang = csr.LoadedModules.Where(m => m.ModuleName == "sqllang").First();
                Assert.NotNull(sqllang);
                Assert.Equal(0x00007FFACB6F0000UL, sqllang.BaseAddress);
                Assert.Equal(0x00007FFAD089FFFFUL, sqllang.EndAddress);
            }
        }

        /// Check the processing of module base address information.
        [Fact]
        public void ModuleLoadAddressInputFullPathSingleLine() {
            using (var csr = new StackResolver()) {
                var moduleAddressesGood = @"c:\mssql\binn\sqldk.dll 0000000100400000";
                Assert.True(csr.ProcessBaseAddresses(moduleAddressesGood));
                Assert.Single(csr.LoadedModules);
                Assert.Equal("sqldk", csr.LoadedModules[0].ModuleName);
            }
        }

        /// Check the processing of module base address information.
        [Fact]
        public void ModuleLoadAddressInputSingleLineBacktick() {
            using (var csr = new StackResolver()) {
                var moduleAddressesGoodBacktick = @"c:\mssql\binn\sqldk.dll 00000001`00400000";
                Assert.True(csr.ProcessBaseAddresses(moduleAddressesGoodBacktick));
                Assert.Single(csr.LoadedModules);
                Assert.Equal("sqldk", csr.LoadedModules[0].ModuleName);
            }
        }

        /// Check the processing of module base address information.
        [Fact]
        public void ModuleLoadAddressInputModuleNameOnlySingleLine() {
            using (var csr = new StackResolver()) {
                var moduleAddressesGoodModuleNameOnly0x = @"sqldk.dll 0000000100400000";
                Assert.True(csr.ProcessBaseAddresses(moduleAddressesGoodModuleNameOnly0x));
                Assert.Single(csr.LoadedModules);
                Assert.Equal("sqldk", csr.LoadedModules[0].ModuleName);
            }
        }

        /// Check the processing of module base address information.
        [Fact]
        public void ModuleLoadAddressInputModuleNameOnlySingleLine0x() {
            using (var csr = new StackResolver()) {
                var moduleAddressesGoodModuleNameOnly0x = @"sqldk.dll 0x0000000100400000";
                Assert.True(csr.ProcessBaseAddresses(moduleAddressesGoodModuleNameOnly0x));
                Assert.Single(csr.LoadedModules);
                Assert.Equal("sqldk", csr.LoadedModules[0].ModuleName);
            }
        }

        /// Check the processing of module base address information.
        [Fact]
        public void ModuleLoadAddressInputFullPathsTwoModules() {
            using (var csr = new StackResolver()) {
                var moduleAddressesGood = @"c:\mssql\binn\sqldk.dll 0000000100400000
                                            c:\mssql\binn\sqllang.dll 0000000105600000";
                Assert.True(csr.ProcessBaseAddresses(moduleAddressesGood));
                Assert.Equal(2, csr.LoadedModules.Count);
                Assert.Equal("sqldk", csr.LoadedModules[0].ModuleName);
                Assert.Equal("sqllang", csr.LoadedModules[1].ModuleName);
            }
        }

        /// Test the resolution of a "regular" symbol with input specifying a hex offset into module
        /// but do not include the resolved symbol's offset in final output.
        [Fact]
        public void RegularSymbolHexOffsetNoOutputOffset() {
            using (var csr = new StackResolver()) {
                var dllPaths = new List<string>{@"..\..\..\Tests\TestCases\TestOrdinal"};
                var ret = csr.ResolveCallstacks("sqldk+0x40609", @"..\..\..\Tests\TestCases\TestOrdinal", false, null, false, false, false, false, false, false, false, null);
                var expectedSymbol = "sqldk!MemoryClerkInternal::AllocatePagesWithFailureMode";
                Assert.Equal(expectedSymbol, ret.Trim());
            }
        }

        /// Test the resolution of a "regular" symbol with input specifying a hex offset into module
        /// but do not include the resolved symbol's offset in final output. This variant has the 
        /// frame numbers prefixed.
        [Fact]
        public void RegularSymbolHexOffsetNoOutputOffsetWithFrameNums() {
            using (var csr = new StackResolver()) {
                var dllPaths = new List<string> { @"..\..\..\Tests\TestCases\TestOrdinal" };
                var ret = csr.ResolveCallstacks("00 sqldk+0x40609", @"..\..\..\Tests\TestCases\TestOrdinal", false, null, false, false, false, false, false, false, false, null);
                var expectedSymbol = "00 sqldk!MemoryClerkInternal::AllocatePagesWithFailureMode";
                Assert.Equal(expectedSymbol, ret.Trim());
            }
        }

        /// Check whether symbol details for a given binary are correct.
        [Fact]
        public void TestGetSymDetails() {
            var dllPaths = new List<string>{@"..\..\..\Tests\TestCases\TestOrdinal"};
            var ret = StackResolver.GetSymbolDetailsForBinaries(dllPaths, true);
            Assert.Single(ret);
            Assert.Equal("https://msdl.microsoft.com/download/symbols/sqldk.pdb/6a1934433512464b8b8ed905ad930ee62/sqldk.pdb", ret[0].DownloadURL);
            Assert.True(ret[0].DownloadVerified);
            Assert.Equal("2015.0130.4560.00 ((SQL16_SP1_QFE-CU).190312-0204)", ret[0].FileVersion);
        }

        /// Make sure that caching PDB files is working. To do this we must use XEL input to trigger multiple worker threads.
        [Fact]
        public void SymbolFileCaching() {
            using (var csr = new StackResolver()) {
                var ret = csr.ExtractFromXEL(new[] { @"..\..\..\Tests\TestCases\ImportXEL\xe_wait_completed_0_132353446563350000.xel" }, false);
                Assert.Equal(550, ret.Item1);
                var status = csr.ProcessBaseAddresses(File.ReadAllText(@"..\..\..\Tests\TestCases\ImportXEL\xe_wait_base_addresses.txt"));
                Assert.True(status);
                Assert.Equal(26, csr.LoadedModules.Count);
                var pdbPath = @"..\..\..\Tests\TestCases\sqlsyms\14.0.3192.2\x64";
                var symres = csr.ResolveCallstacks(ret.Item2, pdbPath, false, null, false, false, false, false, false, false, true, null);
                Assert.Contains(@"sqldk!XeSosPkg::wait_completed::Publish
sqldk!SOS_Scheduler::UpdateWaitTimeStats
sqldk!SOS_Task::PostWait
sqllang!SOS_Task::Sleep
sqllang!YieldAndCheckForAbort
sqllang!OptimizerUtil::YieldAndCheckForMemoryAndAbort
sqllang!OptTypeVRSetArray::IFindSet
sqllang!CConstraintProp::FEquivalent
sqllang!CJoinEdge::FConstrainsColumnSolvably
sqllang!CStCollOuterJoin::CardForColumns
sqllang!CStCollGroupBy::CStCollGroupBy
sqllang!CCardFrameworkSQL12::CardDistinct
sqllang!CCostUtils::CalcLoopJoinCachedInfo
sqllang!CCostUtils::PcctxLoopJoinHelper
sqllang!COpArg::PcctxCalculateNormalizeCtx
sqllang!CTask_OptInputs::Perform
sqllang!CMemo::ExecuteTasks
sqllang!CMemo::PerformOptimizationStage
sqllang!CMemo::OptimizeQuery
sqllang!COptContext::PexprSearchPlan
sqllang!COptContext::PcxteOptimizeQuery
sqllang!COptContext::PqteOptimizeWrapper
sqllang!PqoBuild
sqllang!CStmtQuery::InitQuery", symres.Trim(), StringComparison.CurrentCulture);
            }
        }

        /// Validate that source information is retrieved correctly. This test uses symbols for a Windows Driver Kit module, Wdf01000.sys,
        /// because private PDBs for that module are legitimately available on the Microsoft public symbols servers. https://github.com/microsoft/Windows-Driver-Frameworks/releases if interested.
        [Fact]
        public void SourceInformation() {
            using (var csr = new StackResolver()) {
                var pdbPath = @"..\..\..\Tests\TestCases\SourceInformation";
                var ret = csr.ResolveCallstacks("Wdf01000+17f27", pdbPath, false, null, false, false, true, false, true, false, false, null);
                Assert.Equal("Wdf01000!FxPkgPnp::PowerPolicyCanChildPowerUp+143\t(minkernel\\wdf\\framework\\shared\\inc\\private\\common\\FxPkgPnp.hpp:4127)", ret.Trim());
            }
        }

        /// Validate that source information is retrieved correctly.This test uses symbols for a Windows Driver Kit module, Wdf01000.sys,
        /// because private PDBs for that module are legitimately available on the Microsoft public symbols servers. https://github.com/microsoft/Windows-Driver-Frameworks/releases if interested.
        [Fact]
        public void SourceInformationLineInfoOff() {
            using (var csr = new StackResolver()) {
                var pdbPath = @"..\..\..\Tests\TestCases\SourceInformation";
                var ret = csr.ResolveCallstacks("Wdf01000+17f27", pdbPath, false, null, false, false, false, false, true, false, false, null);
                Assert.Equal("Wdf01000!FxPkgPnp::PowerPolicyCanChildPowerUp+143", ret.Trim());
            }
        }

        /// Validate that source information is retrieved correctly when "re-looking up" a symbol based on input which was already symbolized (but missing source info).
        /// This test uses symbols for a Windows Driver Kit module, Wdf01000.sys, because private PDBs for that module are legitimately available on the Microsoft public symbols servers.https://github.com/microsoft/Windows-Driver-Frameworks/releases if interested.
        [Fact]
        public void RelookupSourceInformation() {
            using (var csr = new StackResolver()) {
                var pdbPath = @"..\..\..\Tests\TestCases\SourceInformation";
                var ret = csr.ResolveCallstacks("Wdf01000!FxPkgPnp::PowerPolicyCanChildPowerUp+143", pdbPath, false, null, false, false, true, true, true, false, false, null);
                Assert.Equal("Wdf01000!FxPkgPnp::PowerPolicyCanChildPowerUp+143\t(minkernel\\wdf\\framework\\shared\\inc\\private\\common\\FxPkgPnp.hpp:4127)", ret.Trim());
            }
        }

        /// Validate importing callstack events from XEL files into histogram buckets.
        [Fact]
        public void ImportBinResolveXELEvents() {
            using (var csr = new StackResolver()) {
                var ret = csr.ExtractFromXEL(new[] { @"..\..\..\Tests\TestCases\ImportXEL\XESpins_0_131627061603030000.xel" }, true);
                Assert.Equal(4, ret.Item1);

                var xmldoc = new XmlDocument() { XmlResolver = null };
                bool isXMLdoc = false;
                try {
                    using (var sreader = new StringReader(ret.Item2)) {
                        using (var reader = XmlReader.Create(sreader, new XmlReaderSettings() { XmlResolver = null })) {
                            xmldoc.Load(reader);
                        }
                    }

                    isXMLdoc = true;
                } catch (XmlException) {
                    // do nothing because this is not a XML doc
                }

                Assert.True(isXMLdoc);
                var slotNodes = xmldoc.SelectNodes("/HistogramTarget/Slot");
                Assert.Equal(4, slotNodes.Count);
                int eventCountFromXML = 0;
                foreach (XmlNode slot in slotNodes) {
                    eventCountFromXML += int.Parse(slot.Attributes["count"].Value, CultureInfo.CurrentCulture);
                }

                Assert.Equal(3051540, eventCountFromXML);
                csr.ProcessBaseAddresses(File.ReadAllText(@"..\..\..\Tests\TestCases\ImportXEL\base_addresses.txt"));
                Assert.Equal(31, csr.LoadedModules.Count);
                var pdbPath = @"..\..\..\Tests\TestCases\sqlsyms\13.0.4001.0\x64";
                var symres = csr.ResolveCallstacks(ret.Item2, pdbPath, false, null, false, false, true, false, true, false, false, null);
                Assert.Contains(@"sqldk!XeSosPkg::spinlock_backoff::Publish+425
sqldk!SpinlockBase::Sleep+182
sqlmin!Spinlock<143,7,1>::SpinToAcquireWithExponentialBackoff+363
sqlmin!lck_lockInternal+2042
sqlmin!MDL::LockGenericLocal+382
sqlmin!MDL::LockGenericIdsLocal+101
sqlmin!CMEDCacheEntryFactory::GetProxiedCacheEntryById+263
sqlmin!CMEDProxyDatabase::GetOwnerByOwnerId+122
sqllang!CSECAccessAuditBase::SetSecurable+427
sqllang!CSECManager::_AccessCheck+151
sqllang!CSECManager::AccessCheck+2346
sqllang!FHasEntityPermissionsWithAuditState+1505
sqllang!FHasEntityPermissions+165
sqllang!CSQLObject::FPostCacheLookup+2562
sqllang!CSQLSource::Transform+2194
sqllang!CSQLSource::Execute+944
sqllang!CStmtExecProc::XretLocalExec+622
sqllang!CStmtExecProc::XretExecExecute+1153
sqllang!CXStmtExecProc::XretExecute+56
sqllang!CMsqlExecContext::ExecuteStmts<1,1>+1037
sqllang!CMsqlExecContext::FExecute+2718
sqllang!CSQLSource::Execute+2435
sqllang!process_request+3681
sqllang!process_commands_internal+735", symres, StringComparison.CurrentCulture);
            }
        }

        /// Validate importing individual callstack events from XEL files.
        [Fact]
        public void ImportIndividualXELEvents() {
            using (var csr = new StackResolver()) {
                var ret = csr.ExtractFromXEL(new[] { @"..\..\..\Tests\TestCases\ImportXEL\xe_wait_completed_0_132353446563350000.xel" }, false);
                Assert.Equal(550, ret.Item1);
            }
        }

        /// Validate importing "single-line" callstack (such as when the input is copy-pasted from SSMS).
        [Fact]
        public void SingleLineCallStack() {
            using (var csr = new StackResolver()) {
                csr.ProcessBaseAddresses(File.ReadAllText(@"..\..\..\Tests\TestCases\ImportXEL\base_addresses.txt"));
                Assert.Equal(31, csr.LoadedModules.Count);
                var pdbPath = @"..\..\..\Tests\TestCases\sqlsyms\13.0.4001.0\x64";
                var callStack = @"callstack	0x00007FFEABD0D919  0x00007FFEABC4D45D  0x00007FFEAC0F7EE0  0x00007FFEAC0F80CF  0x00007FFEAC1EE447  0x00007FFEAC1EE6F5  0x00007FFEAC1D48B0  0x00007FFEAC71475A  0x00007FFEA9A708F1  0x00007FFEA9991FB9  0x00007FFEA9993D21  0x00007FFEA99B59F1  0x00007FFEA99B5055  0x00007FFEA99B2B8F  0x00007FFEA9675AD1  0x00007FFEA9671EFB  0x00007FFEAA37D83D  0x00007FFEAA37D241  0x00007FFEAA379F98  0x00007FFEA96719CA  0x00007FFEA9672933  0x00007FFEA9672041  0x00007FFEA967A82B  0x00007FFEA9681542  ";
                var symres = csr.ResolveCallstacks(callStack, pdbPath, false, null, false, true, true, false, true, false, false, null);
                Assert.Equal(@"callstack
sqldk!XeSosPkg::spinlock_backoff::Publish+425
sqldk!SpinlockBase::Sleep+182
sqlmin!Spinlock<143,7,1>::SpinToAcquireWithExponentialBackoff+363
sqlmin!lck_lockInternal+2042
sqlmin!MDL::LockGenericLocal+382
sqlmin!MDL::LockGenericIdsLocal+101
sqlmin!CMEDCacheEntryFactory::GetProxiedCacheEntryById+263
sqlmin!CMEDProxyDatabase::GetOwnerByOwnerId+122
sqllang!CSECAccessAuditBase::SetSecurable+427
sqllang!CSECManager::_AccessCheck+151
sqllang!CSECManager::AccessCheck+2346
sqllang!FHasEntityPermissionsWithAuditState+1505
sqllang!FHasEntityPermissions+165
sqllang!CSQLObject::FPostCacheLookup+2562
sqllang!CSQLSource::Transform+2194
sqllang!CSQLSource::Execute+944
sqllang!CStmtExecProc::XretLocalExec+622
sqllang!CStmtExecProc::XretExecExecute+1153
sqllang!CXStmtExecProc::XretExecute+56
sqllang!CMsqlExecContext::ExecuteStmts<1,1>+1037
sqllang!CMsqlExecContext::FExecute+2718
sqllang!CSQLSource::Execute+2435
sqllang!process_request+3681
sqllang!process_commands_internal+735", symres.Trim());
            }
        }

        /// Test for inline frame resolution. This test uses symbols for a Windows Driver Kit module, Wdf01000.sys, because private PDBs for that module are legitimately available on the
        /// Microsoft public symbols servers. https://github.com/microsoft/Windows-Driver-Frameworks/releases if interested.
        [Fact]
        public void InlineFrameResolution() {
            using (var csr = new StackResolver()) {
                var pdbPath = @"..\..\..\Tests\TestCases\SourceInformation";
                var ret = csr.ResolveCallstacks("Wdf01000+17f27", pdbPath, false, null, false, false, true, false, true, true, false, null);
                Assert.Equal(
                    @"(Inline Function) Wdf01000!Mx::MxLeaveCriticalRegion+12	(minkernel\wdf\framework\shared\inc\primitives\km\MxGeneralKm.h:198)
(Inline Function) Wdf01000!FxWaitLockInternal::ReleaseLock+62	(minkernel\wdf\framework\shared\inc\private\common\FxWaitLock.hpp:305)
(Inline Function) Wdf01000!FxEnumerationInfo::ReleaseParentPowerStateLock+62	(minkernel\wdf\framework\shared\inc\private\common\FxPkgPnp.hpp:510)
Wdf01000!FxPkgPnp::PowerPolicyCanChildPowerUp+143	(minkernel\wdf\framework\shared\inc\private\common\FxPkgPnp.hpp:4127)",
                    ret.Trim());
            }
        }

        /// Test for inline frame resolution without source lines included This test uses symbols for a Windows Driver Kit module, Wdf01000.sys,
        /// because private PDBs for that module are legitimately available on the Microsoft public symbols servers. https://github.com/microsoft/Windows-Driver-Frameworks/releases if interested.
        [Fact]
        public void InlineFrameResolutionNoSourceInfo() {
            using (var csr = new StackResolver()) {
                var pdbPath = @"..\..\..\Tests\TestCases\SourceInformation";
                var ret = csr.ResolveCallstacks("Wdf01000+17f27", pdbPath, false, null, false, false, false, false, true, true, false, null);
                Assert.Equal(@"(Inline Function) Wdf01000!Mx::MxLeaveCriticalRegion+12
(Inline Function) Wdf01000!FxWaitLockInternal::ReleaseLock+62
(Inline Function) Wdf01000!FxEnumerationInfo::ReleaseParentPowerStateLock+62
Wdf01000!FxPkgPnp::PowerPolicyCanChildPowerUp+143", ret.Trim());
            }
        }

        /// Tests the parsing and extraction of PDB details from a set of rows each with commma-separated fields. This sample mixes up \r\n and \n line-endings.
        [Fact]
        public void ExtractModuleInfo() {
            var ret = ModuleInfoHelper.ParseModuleInfo("\"ntdll.dll\",\"10.0.19041.662\",2056192,666871280,2084960,\"ntdll.pdb\",\"{1EB9FACB-04C7-3C5D-EA71-60764CD333D0}\",0,1\r\n" +
"\"VCRUNTIME140.dll\",\"14.16.27033.0\",86016,1563486943,105788,\"vcruntime140.amd64.pdb\",\"{AF138C3F-2933-4097-8883-C1071B13375E}\",0,1\r\n" +
"\r\n" +
"sqlservr.exe,7ef4ea08-777a-43b7-8bce-4da6f0fa43c7,2\r\n" +
"\"KERNELBASE.dll\",\"10.0.19041.662\",2920448,3965251605,2936791,\"kernelbase.pdb\",\"{1FBE0B2B-89D1-37F0-1510-431FFFBA123E}\",0,1\n" +
"\"kernel32.dll\",\"10.0.19041.662\",774144,1262097423,770204,\"kernel32.pdb\",\"{54448D8E-EFC5-AB3C-7193-D2C7A6DF9008}\",0,1\r\n");

            Assert.Equal(5, ret.Count);
            Assert.Equal("ntdll.pdb", ret["ntdll"].PDBName);
            Assert.Equal("1EB9FACB04C73C5DEA7160764CD333D0", ret["ntdll"].PDBGuid, ignoreCase: true);
            Assert.Equal(1, ret["ntdll"].PDBAge);
            Assert.Equal("vcruntime140.amd64.pdb", ret["VCRUNTIME140"].PDBName);
            Assert.Equal("AF138C3F293340978883C1071B13375E", ret["VCRUNTIME140"].PDBGuid, ignoreCase: true);
            Assert.Equal(1, ret["VCRUNTIME140"].PDBAge);
            Assert.Equal("sqlservr.pdb", ret["sqlservr"].PDBName);
            Assert.Equal("7ef4ea08777a43b78bce4da6f0fa43c7", ret["sqlservr"].PDBGuid, ignoreCase: true);
            Assert.Equal(2, ret["sqlservr"].PDBAge);
        }

        /// Tests the parsing and extraction of PDB details from a set of rows each with XML frames.
        [Fact]
        public void ExtractModuleInfoXMLFrames() {
            var sample = new StackWithCount() {
                Callstack = "Frame = <frame id=\"00\" pdb=\"ntdll.pdb\" age=\"1\" guid=\"C374E059-5793-9B92-6525-386A66A2D3F5\" module=\"ntdll.dll\" rva=\"0x9F7E4\" />        \r\n" +
"Frame = <frame id=\"01\" pdb=\"kernelbase.pdb\" age=\"1\" guid=\"E77E26E7-D1C4-72BB-2C05-DD17624A9E58\" module=\"KERNELBASE.dll\" rva=\"0x38973\" />                          \n" +
"Frame = <frame id=\"02\" pdb=\"SqlDK.pdb\" age=\"2\" guid=\"6a193443-3512-464b-8b8e-\r\n" +
"d905ad930ee6\" module=\"sqldk.dll\" rva=\"0x40609\" />                                    \r\n" +
"<frame id=\"03\" pdb=\"vcruntime140.amd64.pdb\" age=\"1\" guid=\"AF138C3F-2933-4097-8883-C1071B13375E\" module=\"VCRUNTIME140.dll\" rva=\"0xB8F0\" />\r\n" +
"Frame = <frame id=\"04\" pdb=\"SqlDK.pdb\" age=\"2\" guid=\"6a193443-3512-464b-8b8e-d905ad930ee6\" module=\"sqldk.dll\" rva=\"0x2249f\" />                                    \n", Count = 1
            };
            var param = new List<StackWithCount>();
            param.Add(sample);
            var result = ModuleInfoHelper.ParseModuleInfoXML(param);

            var syms = result.Item1;
            Assert.Equal(4, syms.Count);
            Assert.Equal("ntdll.pdb", syms["ntdll"].PDBName);
            Assert.Equal("C374E05957939B926525386A66A2D3F5", syms["ntdll"].PDBGuid, ignoreCase: true);
            Assert.Equal(1, syms["ntdll"].PDBAge);
            Assert.Equal("kernelbase.pdb", syms["KERNELBASE"].PDBName);
            Assert.Equal("E77E26E7D1C472BB2C05DD17624A9E58", syms["KERNELBASE"].PDBGuid, ignoreCase: true);
            Assert.Equal(1, syms["KERNELBASE"].PDBAge);
            Assert.Equal("sqldk.pdb", syms["sqldk"].PDBName);
            Assert.Equal("6a1934433512464b8b8ed905ad930ee6", syms["sqldk"].PDBGuid, ignoreCase: true);
            Assert.Equal(2, syms["sqldk"].PDBAge);
            Assert.Equal("vcruntime140.amd64.pdb", syms["VCRUNTIME140"].PDBName);
            Assert.Equal("AF138C3F293340978883C1071B13375E", syms["VCRUNTIME140"].PDBGuid, ignoreCase: true);
            Assert.Equal(1, syms["VCRUNTIME140"].PDBAge);
        }

        /// Tests the parsing and extraction of PDB details from a set of rows each with commma-separated fields.
        [Fact]
        public void ExtractModuleInfoEmptyString() {
            var ret = ModuleInfoHelper.ParseModuleInfo(string.Empty);
            Assert.Empty(ret);
        }

        /// Test obtaining a local path for symbols downloaded from a symbol server.
        [Fact]
        public void SymSrvLocalPaths() {
            var ret = ModuleInfoHelper.ParseModuleInfo(
                "\"ntdll.dll\",\"10.0.19041.662\",2056192,666871280,2084960,\"ntdll.pdb\",\"{1EB9FACB-04C7-3C5D-EA71-60764CD333D0}\",0,1\r\n" +
"\"VCRUNTIME140.dll\",\"14.16.27033.0\",86016,1563486943,105788,\"vcruntime140.amd64.pdb\",\"{AF138C3F-2933-4097-8883-C1071B13375E}\",0,1\r\n" +
"\r\n" +
"sqlservr.exe,7ef4ea08-777a-43b7-8bce-4da6f0fa43c7,2\r\n" +
"\"KERNELBASE.dll\",\"10.0.19041.662\",2920448,3965251605,2936791,\"kernelbase.pdb\",\"{1FBE0B2B-89D1-37F0-1510-431FFFBA123E}\",0,1\n" +
"\"kernel32.dll\",\"10.0.19041.662\",774144,1262097423,770204,\"kernel32.pdb\",\"{54448D8E-EFC5-AB3C-7193-D2C7A6DF9008}\",0,1\r\n");

            using (var csr = new StackResolver()) {
                var paths = SymSrvHelpers.GetFolderPathsForPDBs(csr, "srv*https://msdl.microsoft.com/download/symbols", ret.Values.ToList());
                Assert.Equal(5, paths.Count);
            }
        }

        /// End-to-end test with stacks being resolved based on symbols from symsrv.
        [Fact]
        public void E2ESymSrv() {
            using (var csr = new StackResolver()) {
                var pdbPath = @"srv*https://msdl.microsoft.com/download/symbols";
                var input = @"ntdll+0x9F7E4
KERNELBASE+0x38973
VCRUNTIME140+0xB8F0
ntdll+0xA479F
ntdll+0x4BEF
ntdll+0x89E6
KERNELBASE+0x396C9
" +
"\"ntdll.dll\",\"10.0.17763.1490\",2019328,462107166,2009368,\"ntdll.pdb\",\"{C374E059-5793-9B92-6525-386A66A2D3F5}\",0,1\r\n" +
"\"KERNELBASE.dll\",\"10.0.17763.1518\",2707456,4281343292,2763414,\"kernelbase.pdb\",\"{E77E26E7-D1C4-72BB-2C05-DD17624A9E58}\",0,1\r\n" +
"\"VCRUNTIME140.dll\",\"14.16.27033.0\",86016,1563486943,105788,\"vcruntime140.amd64.pdb\",\"{AF138C3F-2933-4097-8883-C1071B13375E}\",0,1\r\n";

                var ret = csr.ResolveCallstacks(input, pdbPath, false, null, false, false, true, false, true, false, false, null);
                var expected = @"ntdll!NtWaitForSingleObject+20
KERNELBASE!WaitForSingleObjectEx+147
VCRUNTIME140!__C_specific_handler+160	(d:\agent\_work\2\s\src\vctools\crt\vcruntime\src\eh\riscchandler.cpp:290)
ntdll!RtlpExecuteHandlerForException+15
ntdll!RtlDispatchException+1039
ntdll!RtlRaiseException+790
KERNELBASE!RaiseException+105
" +
"\"ntdll.dll\",\"10.0.17763.1490\",2019328,462107166,2009368,\"ntdll.pdb\",\"{C374E059-5793-9B92-6525-386A66A2D3F5}\",0,1\r\n" +
"\"KERNELBASE.dll\",\"10.0.17763.1518\",2707456,4281343292,2763414,\"kernelbase.pdb\",\"{E77E26E7-D1C4-72BB-2C05-DD17624A9E58}\",0,1\r\n" +
"\"VCRUNTIME140.dll\",\"14.16.27033.0\",86016,1563486943,105788,\"vcruntime140.amd64.pdb\",\"{AF138C3F-2933-4097-8883-C1071B13375E}\",0,1";
                Assert.Equal(expected.Trim(), ret.Trim());
            }
        }

        /// End-to-end test with stacks being resolved based on symbols from symsrv, but just one line of input
        [Fact]
        public void E2ESymSrvXMLSingleFrame() {
            using (var csr = new StackResolver()) {
                var pdbPath = @"srv*https://msdl.microsoft.com/download/symbols";
                var input = "<frame id=\"00\" pdb=\"ntdll.pdb\" age=\"1\" guid=\"C374E059-5793-9B92-6525-386A66A2D3F5\" module=\"ntdll.dll\" rva=\"0x9F7E4\" />";
                var ret = csr.ResolveCallstacks(input, pdbPath, false, null, false, false, true, false, true, false, false, null);
                var expected = @"00 ntdll!NtWaitForSingleObject+20";
                Assert.Equal(expected.Trim(), ret.Trim());
            }
        }

        /// End-to-end test with stacks being resolved based on symbols from symsrv.
        [Fact]
        public void E2ESymSrvXMLFrames() {
            using (var csr = new StackResolver()) {
                var pdbPath = @"srv*https://msdl.microsoft.com/download/symbols";
                var input = "Frame = <frame id=\"00\" pdb=\"ntdll.pdb\" age=\"1\" guid=\"C374E059-5793-9B92-6525-386A66A2D3F5\" module=\"ntdll.dll\" rva=\"0x9F7E4\" />        \r\n" +
"Frame = <frame id=\"01\" pdb=\"kernelbase.pdb\" age=\"1\" guid=\"E77E26E7-D1C4-72BB-2C05-DD17624A9E58\" module=\"KERNELBASE.dll\" rva=\"0x38973\" />                          \n" +
"Frame = <frame id=\"02\" pdb=\"SqlDK.pdb\" age=\"2\" guid=\"6a193443-3512-464b-8b8e-\r\n" +
"d905ad930ee6\" module=\"sqldk.dll\" rva=\"0x40609\" />                                    \r\n" +
"<frame id=\"03\" pdb=\"vcruntime140.amd64.pdb\" age=\"1\" guid=\"AF138C3F-2933-4097-8883-C1071B13375E\" module=\"VCRUNTIME140.dll\" rva=\"0xB8F0\" />\r\n" +
"Frame = <frame id=\"04\" pdb=\"SqlDK.pdb\" age=\"2\" guid=\"6a193443-3512-464b-8b8e-d905ad930ee6\" module=\"sqldk.dll\" rva=\"0x2249f\" />                                    \n";

                var ret = csr.ResolveCallstacks(input, pdbPath, false, null, false, false, true, false, true, false, false, null);
                var expected = @"00 ntdll!NtWaitForSingleObject+20
01 KERNELBASE!WaitForSingleObjectEx+147
02 sqldk!MemoryClerkInternal::AllocatePagesWithFailureMode+644
03 VCRUNTIME140!__C_specific_handler+160	(d:\agent\_work\2\s\src\vctools\crt\vcruntime\src\eh\riscchandler.cpp:290)
04 sqldk!Spinlock<244,2,1>::SpinToAcquireWithExponentialBackoff+349";
                Assert.Equal(expected.Trim(), ret.Trim());
            }
        }

        /// End-to-end test with XE histogram target and XML frames.
        [Fact]
        public void E2ESymSrvXMLFramesHistogram() {
            using (var csr = new StackResolver()) {
                var pdbPath = @"srv*https://msdl.microsoft.com/download/symbols";
                var input = "<HistogramTarget truncated=\"0\" buckets=\"256\"><Slot count=\"5\"><value><![CDATA[<frame id=\"00\" pdb=\"ntdll.pdb\" age=\"1\" guid=\"C374E059-5793-9B92-6525-386A66A2D3F5\" module=\"ntdll.dll\" rva=\"0x9F7E4\" />" +
"<frame id=\"01\" pdb=\"kernelbase.pdb\" age=\"1\" guid=\"E77E26E7-D1C4-72BB-2C05-DD17624A9E58\" module=\"KERNELBASE.dll\" rva=\"0x38973\" />" +
"<frame id=\"02\" pdb=\"SqlDK.pdb\" age=\"2\" guid=\"6a193443-3512-464b-8b8e-d905ad930ee6\" module=\"sqldk.dll\" rva=\"0x40609\" />" +
"]]></value></Slot><Slot count=\"3\"><value><![CDATA[<frame id=\"00\" pdb=\"vcruntime140.amd64.pdb\" age=\"1\" guid=\"AF138C3F-2933-4097-8883-C1071B13375E\" module=\"VCRUNTIME140.dll\" rva=\"0xB8F0\" />" +
"<frame id=\"01\" pdb=\"SqlDK.pdb\" age=\"2\" guid=\"6a193443-3512-464b-8b8e-d905ad930ee6\" module=\"sqldk.dll\" rva=\"0x2249f\" />" +
"]]></value></Slot></HistogramTarget>";

                var ret = csr.ResolveCallstacks(input, pdbPath, false, null, false, false, true, false, true, false, false, null);
                var expected = @"Slot_0	[count:5]:

00 ntdll!NtWaitForSingleObject+20
01 KERNELBASE!WaitForSingleObjectEx+147
02 sqldk!MemoryClerkInternal::AllocatePagesWithFailureMode+644

Slot_1	[count:3]:

00 VCRUNTIME140!__C_specific_handler+160	(d:\agent\_work\2\s\src\vctools\crt\vcruntime\src\eh\riscchandler.cpp:290)
01 sqldk!Spinlock<244,2,1>::SpinToAcquireWithExponentialBackoff+349";
                Assert.Equal(expected.Trim(), ret.Trim());
            }
        }

        /// End-to-end test with stacks being resolved based on symbols from symsrv.
        [Fact]
        public void E2ESymSrvXMLFramesMixedLineEndings() {
            using (var csr = new StackResolver()) {
                var pdbPath = @"srv*https://msdl.microsoft.com/download/symbols";
                var input = "<frame id=\"00\" pdb=\"ntdll.pdb\" age=\"1\" guid=\"C374E059-5793-9B92-6525-386A66A2D3F5\" module=\"ntdll.dll\" rva=\"0x9F7E4\" />" +
"<frame id=\"01\" pdb=\"kernelbase.pdb\" age=\"1\" guid=\"E77E26E7-D1C4-72BB-2C05-DD17624A9E58\" module=\"KERNELBASE.dll\" rva=\"0x38973\" />" +
"<frame id=\"02\" pdb=\"SqlDK.pdb\" age=\"2\" guid=\"6a193443-3512-464b-8b8e-\r\n" +
"d905ad930ee6\" module=\"sqldk.dll\" rva=\"0x40609\" />                                    \r\n" +
"<frame id=\"03\" pdb=\"vcruntime140.amd64.pdb\" age=\"1\" guid=\"AF138C3F-2933-4097-8883-C1071B13375E\" module=\"VCRUNTIME140.dll\" rva=\"0xB8F0\" />" +
"<frame id=\"04\" pdb=\"SqlDK.pdb\" age=\"2\" guid=\"6a193443-3512-464b-8b8e-d905ad930ee6\" module=\"sqldk.dll\" rva=\"0x2249f\" />                                    \n";

                var ret = csr.ResolveCallstacks(input, pdbPath, false, null, false, false, true, false, true, false, false, null);
                var expected = @"00 ntdll!NtWaitForSingleObject+20
01 KERNELBASE!WaitForSingleObjectEx+147
02 sqldk!MemoryClerkInternal::AllocatePagesWithFailureMode+644
03 VCRUNTIME140!__C_specific_handler+160	(d:\agent\_work\2\s\src\vctools\crt\vcruntime\src\eh\riscchandler.cpp:290)
04 sqldk!Spinlock<244,2,1>::SpinToAcquireWithExponentialBackoff+349

";
                Assert.Equal(expected.Trim(), ret.Trim());
            }
        }

        /// End-to-end test with stacks being resolved based on symbols from symsrv.
        [Fact]
        public void E2ESymSrvNoSympath() {
            using (var csr = new StackResolver()) {
                var pdbPath = string.Empty;
                var input = @"ntdll+0x9F7E4
KERNELBASE+0x38973
VCRUNTIME140+0xB8F0
ntdll+0xA479F
ntdll+0x4BEF
ntdll+0x89E6
KERNELBASE+0x396C9
" +
"\"ntdll.dll\",\"10.0.17763.1490\",2019328,462107166,2009368,\"ntdll.pdb\",\"{C374E059-5793-9B92-6525-386A66A2D3F5}\",0,1\r\n" +
"\"KERNELBASE.dll\",\"10.0.17763.1518\",2707456,4281343292,2763414,\"kernelbase.pdb\",\"{E77E26E7-D1C4-72BB-2C05-DD17624A9E58}\",0,1\r\n" +
"\"VCRUNTIME140.dll\",\"14.16.27033.0\",86016,1563486943,105788,\"vcruntime140.amd64.pdb\",\"{AF138C3F-2933-4097-8883-C1071B13375E}\",0,1\r\n";

                var ret = csr.ResolveCallstacks(input, pdbPath, false, null, false, false, true, false, true, false, false, null);
                Assert.Equal(input.Trim(), ret.Trim());
            }
        }

        /// Test for exported symbols
        [Fact]
        public void CheckExportedSymbols() {
            using (var csr = new StackResolver()) {
                var ret = ExportedSymbol.GetExports(@"..\..\..\Tests\TestCases\TestOrdinal\sqldk.dll");
                Assert.Equal(931, ret.Count);
                Assert.Equal((uint)1095072, ret[15].Address);
                Assert.Equal((uint)897568, ret[259].Address);
                Assert.Equal((uint)58752, ret[684].Address);
                Assert.Equal((uint)1447120, ret[1161].Address);
            }
        }
    }
}
