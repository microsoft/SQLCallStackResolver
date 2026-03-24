// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
using Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver.Modern;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver.Modern.Tests {
    [TestClass]
    public class ViewModelTests {

        // ----------------------------------------------------------------
        // Initial state
        // ----------------------------------------------------------------

        [TestMethod][TestCategory("ModernUI")]
        public void InitialState_HasFourCoreSteps() {
            var vm = new ResolverViewModel();
            Assert.AreEqual(4, vm.WizardSteps.Count);
            Assert.AreEqual("InputSource", vm.WizardSteps[0].Id);
            Assert.AreEqual("Symbols", vm.WizardSteps[1].Id);
            Assert.AreEqual("Options", vm.WizardSteps[2].Id);
            Assert.AreEqual("Resolve", vm.WizardSteps[3].Id);
        }

        [TestMethod][TestCategory("ModernUI")]
        public void InitialState_NavigationProperties() {
            var vm = new ResolverViewModel();
            Assert.AreEqual(0, vm.CurrentStep);
            Assert.AreEqual("InputSource", vm.CurrentStepId);
            Assert.IsFalse(vm.CanGoBack);
            Assert.IsTrue(vm.CanGoNext);
            Assert.IsFalse(vm.IsOnResolvePage);
            Assert.IsFalse(vm.IsOnStepBeforeResolve);
            Assert.IsFalse(vm.ShowNextButton, "Next should be hidden on InputSource");
        }

        [TestMethod][TestCategory("ModernUI")]
        public void InitialState_DefaultPropertyValues() {
            var vm = new ResolverViewModel();
            Assert.AreEqual(string.Empty, vm.InputText);
            Assert.AreEqual(string.Empty, vm.OutputText);
            Assert.AreEqual(string.Empty, vm.BaseAddressesString);
            Assert.AreEqual(string.Empty, vm.PdbPaths);
            Assert.IsFalse(vm.FramesOnSingleLine);
            Assert.IsFalse(vm.IsProcessing);
            Assert.IsTrue(vm.IsNotProcessing);
            Assert.AreEqual("Ready", vm.StatusMessage);
            Assert.AreEqual(0, vm.ProgressPercent);
        }

        // ----------------------------------------------------------------
        // Step navigation via CurrentStep
        // ----------------------------------------------------------------

        [TestMethod][TestCategory("ModernUI")]
        public void CurrentStep_SetToLast_IsOnResolvePage() {
            var vm = new ResolverViewModel();
            vm.CurrentStep = vm.WizardSteps.Count - 1;
            Assert.IsTrue(vm.IsOnResolvePage);
            Assert.IsFalse(vm.CanGoNext);
            Assert.IsTrue(vm.CanGoBack);
        }

        [TestMethod][TestCategory("ModernUI")]
        public void CurrentStep_SetToSecondToLast_IsOnStepBeforeResolve() {
            var vm = new ResolverViewModel();
            vm.CurrentStep = vm.WizardSteps.Count - 2;
            Assert.IsTrue(vm.IsOnStepBeforeResolve);
            Assert.IsFalse(vm.IsOnResolvePage);
        }

        [TestMethod][TestCategory("ModernUI")]
        public void ShowNextButton_HiddenOnInputSource() {
            var vm = new ResolverViewModel();
            vm.CurrentStep = 0;
            Assert.AreEqual("InputSource", vm.CurrentStepId);
            Assert.IsFalse(vm.ShowNextButton);
        }

        [TestMethod][TestCategory("ModernUI")]
        public void ShowNextButton_VisibleOnMiddleSteps() {
            var vm = new ResolverViewModel();
            // Navigate to Symbols (index 1 in default 4-step layout)
            vm.CurrentStep = 1;
            Assert.AreEqual("Symbols", vm.CurrentStepId);
            Assert.IsTrue(vm.ShowNextButton);
        }

        [TestMethod][TestCategory("ModernUI")]
        public void ShowNextButton_HiddenOnStepBeforeResolve() {
            var vm = new ResolverViewModel();
            vm.CurrentStep = vm.WizardSteps.Count - 2; // Options
            Assert.IsFalse(vm.ShowNextButton);
        }

        // ----------------------------------------------------------------
        // PropertyChanged notifications
        // ----------------------------------------------------------------

        [TestMethod][TestCategory("ModernUI")]
        public void CurrentStep_RaisesExpectedPropertyChangedEvents() {
            var vm = new ResolverViewModel();
            var changed = new List<string>();
            vm.PropertyChanged += (s, e) => changed.Add(e.PropertyName);

            vm.CurrentStep = 1;

            Assert.IsTrue(changed.Contains(nameof(vm.CurrentStep)));
            Assert.IsTrue(changed.Contains(nameof(vm.CanGoBack)));
            Assert.IsTrue(changed.Contains(nameof(vm.CanGoNext)));
            Assert.IsTrue(changed.Contains(nameof(vm.IsOnResolvePage)));
            Assert.IsTrue(changed.Contains(nameof(vm.IsOnStepBeforeResolve)));
            Assert.IsTrue(changed.Contains(nameof(vm.ShowNextButton)));
            Assert.IsTrue(changed.Contains(nameof(vm.CurrentStepId)));
        }

        [TestMethod][TestCategory("ModernUI")]
        public void InputText_RaisesPropertyChanged() {
            var vm = new ResolverViewModel();
            string changedProp = null;
            vm.PropertyChanged += (s, e) => changedProp = e.PropertyName;
            vm.InputText = "test callstack";
            Assert.AreEqual(nameof(vm.InputText), changedProp);
        }

        [TestMethod][TestCategory("ModernUI")]
        public void IsProcessing_AlsoRaisesIsNotProcessing() {
            var vm = new ResolverViewModel();
            var changed = new List<string>();
            vm.PropertyChanged += (s, e) => changed.Add(e.PropertyName);
            vm.IsProcessing = true;
            Assert.IsTrue(changed.Contains(nameof(vm.IsProcessing)));
            Assert.IsTrue(changed.Contains(nameof(vm.IsNotProcessing)));
            Assert.IsTrue(vm.IsProcessing);
            Assert.IsFalse(vm.IsNotProcessing);
        }

        // ----------------------------------------------------------------
        // InsertSubStepAfter
        // ----------------------------------------------------------------

        [TestMethod][TestCategory("ModernUI")]
        public void InsertSubStep_InsertsAfterParent() {
            var vm = new ResolverViewModel();
            vm.InsertSubStepAfter("InputSource", ResolverViewModel.StepInput);
            Assert.AreEqual(5, vm.WizardSteps.Count);
            Assert.AreEqual("InputSource", vm.WizardSteps[0].Id);
            Assert.AreEqual("Input", vm.WizardSteps[1].Id);
            Assert.AreEqual("Symbols", vm.WizardSteps[2].Id);
        }

        [TestMethod][TestCategory("ModernUI")]
        public void InsertSubStep_DuplicateIsIgnored() {
            var vm = new ResolverViewModel();
            vm.InsertSubStepAfter("InputSource", ResolverViewModel.StepInput);
            vm.InsertSubStepAfter("InputSource", ResolverViewModel.StepInput);
            Assert.AreEqual(5, vm.WizardSteps.Count);
            Assert.AreEqual(1, vm.WizardSteps.Count(s => s.Id == "Input"));
        }

        [TestMethod][TestCategory("ModernUI")]
        public void InsertSubStep_MultipleSiblingsInsertedInOrder() {
            var vm = new ResolverViewModel();
            vm.InsertSubStepAfter("InputSource", ResolverViewModel.StepInput);
            vm.InsertSubStepAfter("InputSource", ResolverViewModel.StepBaseAddress);
            // Both should be after InputSource, before Symbols
            Assert.AreEqual(6, vm.WizardSteps.Count);
            Assert.AreEqual("InputSource", vm.WizardSteps[0].Id);
            Assert.AreEqual("Input", vm.WizardSteps[1].Id);
            Assert.AreEqual("BaseAddress", vm.WizardSteps[2].Id);
            Assert.AreEqual("Symbols", vm.WizardSteps[3].Id);
        }

        [TestMethod][TestCategory("ModernUI")]
        public void InsertSubStep_InvalidParent_NoChange() {
            var vm = new ResolverViewModel();
            vm.InsertSubStepAfter("NonExistent", ResolverViewModel.StepInput);
            Assert.AreEqual(4, vm.WizardSteps.Count);
        }

        [TestMethod][TestCategory("ModernUI")]
        public void InsertSubStep_UpdatesTotalSteps() {
            var vm = new ResolverViewModel();
            Assert.AreEqual(4, vm.TotalSteps);
            vm.InsertSubStepAfter("InputSource", ResolverViewModel.StepInput);
            Assert.AreEqual(5, vm.TotalSteps);
        }

        // ----------------------------------------------------------------
        // RemoveSubStep
        // ----------------------------------------------------------------

        [TestMethod][TestCategory("ModernUI")]
        public void RemoveSubStep_RemovesExistingStep() {
            var vm = new ResolverViewModel();
            vm.InsertSubStepAfter("InputSource", ResolverViewModel.StepInput);
            Assert.AreEqual(5, vm.WizardSteps.Count);
            vm.RemoveSubStep("Input");
            Assert.AreEqual(4, vm.WizardSteps.Count);
            Assert.IsFalse(vm.WizardSteps.Any(s => s.Id == "Input"));
        }

        [TestMethod][TestCategory("ModernUI")]
        public void RemoveSubStep_NonExistent_NoChange() {
            var vm = new ResolverViewModel();
            vm.RemoveSubStep("NonExistent");
            Assert.AreEqual(4, vm.WizardSteps.Count);
        }

        [TestMethod][TestCategory("ModernUI")]
        public void RemoveSubStep_ClampsCurrentStep() {
            var vm = new ResolverViewModel();
            vm.InsertSubStepAfter("InputSource", ResolverViewModel.StepInput);
            // 5 steps: InputSource(0), Input(1), Symbols(2), Options(3), Resolve(4)
            vm.CurrentStep = 4; // on Resolve
            vm.RemoveSubStep("Input");
            // Now 4 steps: InputSource(0), Symbols(1), Options(2), Resolve(3)
            Assert.IsTrue(vm.CurrentStep < vm.WizardSteps.Count);
        }

        [TestMethod][TestCategory("ModernUI")]
        public void HasSubStep_ReturnsTrueForExisting() {
            var vm = new ResolverViewModel();
            Assert.IsTrue(vm.HasSubStep("InputSource"));
            Assert.IsTrue(vm.HasSubStep("Symbols"));
            Assert.IsFalse(vm.HasSubStep("Input"));
            vm.InsertSubStepAfter("InputSource", ResolverViewModel.StepInput);
            Assert.IsTrue(vm.HasSubStep("Input"));
        }

        // ----------------------------------------------------------------
        // StartOver (via StartOverCommand)
        // ----------------------------------------------------------------

        [TestMethod][TestCategory("ModernUI")]
        public void StartOver_ResetsAllInputsAndSteps() {
            var vm = new ResolverViewModel();
            // Set up state
            vm.InsertSubStepAfter("InputSource", ResolverViewModel.StepInput);
            vm.InsertSubStepAfter("InputSource", ResolverViewModel.StepFieldSelection);
            vm.InsertSubStepAfter("InputSource", ResolverViewModel.StepBaseAddress);
            vm.InputText = "some input";
            vm.OutputText = "some output";
            vm.BaseAddressesString = "some addresses";
            vm.StatusMessage = "Processing...";
            vm.ProgressPercent = 50;
            vm.CurrentStep = 3;

            // Execute StartOver via the command
            vm.StartOverCommand.Execute(null);

            Assert.AreEqual(string.Empty, vm.InputText);
            Assert.AreEqual(string.Empty, vm.OutputText);
            Assert.AreEqual(string.Empty, vm.BaseAddressesString);
            Assert.IsFalse(vm.FramesOnSingleLine);
            Assert.IsFalse(vm.RelookupSource);
            Assert.AreEqual("Ready", vm.StatusMessage);
            Assert.AreEqual(0, vm.ProgressPercent);
            Assert.AreEqual(0, vm.CurrentStep);
            Assert.AreEqual(4, vm.WizardSteps.Count, "All conditional sub-steps should be removed");
            Assert.IsNull(vm.PendingXELFileNames);
        }

        // ----------------------------------------------------------------
        // ValidateInputs
        // ----------------------------------------------------------------

        [TestMethod][TestCategory("ModernUI")]
        public void ValidateInputs_NoPdbPaths_ReturnsError() {
            var vm = new ResolverViewModel();
            vm.PdbPaths = "";
            vm.InputText = "sqldk!SOS_Scheduler+0x100";
            var result = vm.ValidateInputs();
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Contains("PDB"));
        }

        [TestMethod][TestCategory("ModernUI")]
        public void ValidateInputs_WithPdbPaths_NoError() {
            var vm = new ResolverViewModel();
            vm.PdbPaths = @"C:\symbols";
            vm.InputText = "sqldk!SOS_Scheduler+0x100";
            var result = vm.ValidateInputs();
            Assert.IsNull(result);
        }

        [TestMethod][TestCategory("ModernUI")]
        public void ValidateInputs_VAOnlyWithoutBaseAddresses_ReturnsError() {
            var vm = new ResolverViewModel();
            vm.PdbPaths = @"C:\symbols";
            vm.InputText = "0x00007FF830D4CDA4";
            vm.BaseAddressesString = "";
            var result = vm.ValidateInputs();
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Contains("virtual addresses") || result.Contains("base address"));
        }

        [TestMethod][TestCategory("ModernUI")]
        public void ValidateInputs_SingleLineInputMismatch_ReturnsError() {
            var vm = new ResolverViewModel();
            vm.PdbPaths = @"C:\symbols";
            // Multi-line input with FramesOnSingleLine checked → mismatch
            vm.InputText = "sqldk!SOS_Scheduler+0x100\r\nsqldk!SOS_Worker+0x200";
            vm.FramesOnSingleLine = true;
            var result = vm.ValidateInputs();
            Assert.IsNotNull(result, "Should detect mismatch: multi-line input but FramesOnSingleLine checked");
            Assert.IsTrue(result.Contains("single line") || result.Contains("single-line") || result.Contains("Single"));
        }

        // ----------------------------------------------------------------
        // SubStepAction event
        // ----------------------------------------------------------------

        [TestMethod][TestCategory("ModernUI")]
        public void RaiseSubStepAction_FiresEvent() {
            var vm = new ResolverViewModel();
            string received = null;
            vm.SubStepAction += action => received = action;
            vm.RaiseSubStepAction("ChooseDirectInput");
            Assert.AreEqual("ChooseDirectInput", received);
        }

        [TestMethod][TestCategory("ModernUI")]
        public void RaiseSubStepAction_NoSubscriber_DoesNotThrow() {
            var vm = new ResolverViewModel();
            vm.RaiseSubStepAction("ChooseDirectInput"); // no subscriber, should not throw
        }

        // ----------------------------------------------------------------
        // WizardStep model
        // ----------------------------------------------------------------

        [TestMethod][TestCategory("ModernUI")]
        public void WizardStep_CoreStepsAreNotConditional() {
            Assert.IsFalse(ResolverViewModel.StepInputSource.IsConditional);
            Assert.IsFalse(ResolverViewModel.StepSymbols.IsConditional);
            Assert.IsFalse(ResolverViewModel.StepOptions.IsConditional);
            Assert.IsFalse(ResolverViewModel.StepResolve.IsConditional);
        }

        [TestMethod][TestCategory("ModernUI")]
        public void WizardStep_ConditionalStepsHaveParent() {
            Assert.IsTrue(ResolverViewModel.StepInput.IsConditional);
            Assert.AreEqual("InputSource", ResolverViewModel.StepInput.ParentStepId);
            Assert.IsTrue(ResolverViewModel.StepFieldSelection.IsConditional);
            Assert.AreEqual("InputSource", ResolverViewModel.StepFieldSelection.ParentStepId);
            Assert.IsTrue(ResolverViewModel.StepBaseAddress.IsConditional);
            Assert.AreEqual("InputSource", ResolverViewModel.StepBaseAddress.ParentStepId);
        }

        // ----------------------------------------------------------------
        // Command CanExecute
        // ----------------------------------------------------------------

        [TestMethod][TestCategory("ModernUI")]
        public void ResolveCommand_CanExecute_WhenNotProcessing() {
            var vm = new ResolverViewModel();
            Assert.IsTrue(vm.ResolveCommand.CanExecute(null));
            vm.IsProcessing = true;
            Assert.IsFalse(vm.ResolveCommand.CanExecute(null));
        }

        [TestMethod][TestCategory("ModernUI")]
        public void CancelCommand_CanExecute_OnlyWhenProcessing() {
            var vm = new ResolverViewModel();
            Assert.IsFalse(vm.CancelCommand.CanExecute(null));
            vm.IsProcessing = true;
            Assert.IsTrue(vm.CancelCommand.CanExecute(null));
        }

        [TestMethod][TestCategory("ModernUI")]
        public void StartOverCommand_CanExecute_WhenNotProcessing() {
            var vm = new ResolverViewModel();
            Assert.IsTrue(vm.StartOverCommand.CanExecute(null));
            vm.IsProcessing = true;
            Assert.IsFalse(vm.StartOverCommand.CanExecute(null));
        }

        [TestMethod][TestCategory("ModernUI")]
        public void NextStepCommand_CanExecute_NotOnLastStep() {
            var vm = new ResolverViewModel();
            Assert.IsTrue(vm.NextStepCommand.CanExecute(null));
            vm.CurrentStep = vm.WizardSteps.Count - 1;
            Assert.IsFalse(vm.NextStepCommand.CanExecute(null));
        }

        [TestMethod][TestCategory("ModernUI")]
        public void PreviousStepCommand_CanExecute_NotOnFirstStep() {
            var vm = new ResolverViewModel();
            Assert.IsFalse(vm.PreviousStepCommand.CanExecute(null));
            vm.CurrentStep = 1;
            Assert.IsTrue(vm.PreviousStepCommand.CanExecute(null));
        }

        // ----------------------------------------------------------------
        // Navigation through steps (integrated scenarios)
        // ----------------------------------------------------------------

        [TestMethod][TestCategory("ModernUI")]
        public void DirectInputFlow_InsertInputStep_NavigateThrough() {
            var vm = new ResolverViewModel();
            // Simulate "ChooseDirectInput": insert Input step
            vm.InsertSubStepAfter("InputSource", ResolverViewModel.StepInput);
            // Steps: InputSource(0), Input(1), Symbols(2), Options(3), Resolve(4)
            Assert.AreEqual(5, vm.WizardSteps.Count);

            vm.CurrentStep = 1;
            Assert.AreEqual("Input", vm.CurrentStepId);
            Assert.IsTrue(vm.ShowNextButton);

            vm.CurrentStep = 2;
            Assert.AreEqual("Symbols", vm.CurrentStepId);
            Assert.IsTrue(vm.ShowNextButton);

            vm.CurrentStep = 3;
            Assert.AreEqual("Options", vm.CurrentStepId);
            Assert.IsFalse(vm.ShowNextButton, "Should be hidden on step before Resolve");
            Assert.IsTrue(vm.IsOnStepBeforeResolve);

            vm.CurrentStep = 4;
            Assert.AreEqual("Resolve", vm.CurrentStepId);
            Assert.IsTrue(vm.IsOnResolvePage);
        }

        [TestMethod][TestCategory("ModernUI")]
        public void XELFlow_InsertFieldSelection_ThenBaseAddress() {
            var vm = new ResolverViewModel();
            // Simulate "ChooseXELImport"
            vm.InsertSubStepAfter("InputSource", ResolverViewModel.StepFieldSelection);
            Assert.AreEqual(5, vm.WizardSteps.Count);
            Assert.AreEqual("FieldSelection", vm.WizardSteps[1].Id);

            // Simulate XEL import reveals VA-only data → insert BaseAddress
            vm.InsertSubStepAfter("InputSource", ResolverViewModel.StepBaseAddress);
            Assert.AreEqual(6, vm.WizardSteps.Count);
            // Order: InputSource, FieldSelection, BaseAddress, Symbols, Options, Resolve
            Assert.AreEqual("FieldSelection", vm.WizardSteps[1].Id);
            Assert.AreEqual("BaseAddress", vm.WizardSteps[2].Id);
            Assert.AreEqual("Symbols", vm.WizardSteps[3].Id);
        }

        [TestMethod][TestCategory("ModernUI")]
        public void SwitchingPaths_RemovesOldSubSteps() {
            var vm = new ResolverViewModel();
            // Start with direct input path
            vm.InsertSubStepAfter("InputSource", ResolverViewModel.StepInput);
            Assert.IsTrue(vm.HasSubStep("Input"));

            // Switch to XEL path: remove Input, add FieldSelection
            vm.RemoveSubStep("Input");
            vm.InsertSubStepAfter("InputSource", ResolverViewModel.StepFieldSelection);
            Assert.IsFalse(vm.HasSubStep("Input"));
            Assert.IsTrue(vm.HasSubStep("FieldSelection"));
            Assert.AreEqual(5, vm.WizardSteps.Count);
        }
    }
}
