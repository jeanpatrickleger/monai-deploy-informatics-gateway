﻿// ------------------------------------------------------------------------------
//  <auto-generated>
//      This code was generated by SpecFlow (https://www.specflow.org/).
//      SpecFlow Version:3.9.0.0
//      SpecFlow Generator Version:3.9.0.0
// 
//      Changes to this file may cause incorrect behavior and will be lost if
//      the code is regenerated.
//  </auto-generated>
// ------------------------------------------------------------------------------
#region Designer generated code
#pragma warning disable
namespace Monai.Deploy.InformaticsGateway.Integration.Test.Features
{
    using TechTalk.SpecFlow;
    using System;
    using System.Linq;
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("TechTalk.SpecFlow", "3.9.0.0")]
    [System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [Xunit.TraitAttribute("Category", "scp")]
    public partial class DICOMDIMSESCUServicesFeature : object, Xunit.IClassFixture<DICOMDIMSESCUServicesFeature.FixtureData>, System.IDisposable
    {
        
        private static TechTalk.SpecFlow.ITestRunner testRunner;
        
        private string[] _featureTags = new string[] {
                "scp"};
        
        private Xunit.Abstractions.ITestOutputHelper _testOutputHelper;
        
#line 1 "DicomDimseScu.feature"
#line hidden
        
        public DICOMDIMSESCUServicesFeature(DICOMDIMSESCUServicesFeature.FixtureData fixtureData, Monai_Deploy_InformaticsGateway_Integration_Test_XUnitAssemblyFixture assemblyFixture, Xunit.Abstractions.ITestOutputHelper testOutputHelper)
        {
            this._testOutputHelper = testOutputHelper;
            this.TestInitialize();
        }
        
        public static void FeatureSetup()
        {
            testRunner = TechTalk.SpecFlow.TestRunnerManager.GetTestRunner();
            TechTalk.SpecFlow.FeatureInfo featureInfo = new TechTalk.SpecFlow.FeatureInfo(new System.Globalization.CultureInfo("en-US"), "Features", "DICOM DIMSE SCU Services", @"    This feature tests the DIMSE services provided by the Informatics Gateway as a SCU.
    Requirements covered:
    - [REQ-DCM-03] MIG SHALL be able to export DICOM via C-STORE
    - [REQ-DCM-09] Store SCU AE Title shall be configurable
    - [REQ-DCM-11] MIG SHALL support exporting data to multiple DICOM destinations", ProgrammingLanguage.CSharp, new string[] {
                        "scp"});
            testRunner.OnFeatureStart(featureInfo);
        }
        
        public static void FeatureTearDown()
        {
            testRunner.OnFeatureEnd();
            testRunner = null;
        }
        
        public virtual void TestInitialize()
        {
        }
        
        public virtual void TestTearDown()
        {
            testRunner.OnScenarioEnd();
        }
        
        public virtual void ScenarioInitialize(TechTalk.SpecFlow.ScenarioInfo scenarioInfo)
        {
            testRunner.OnScenarioInitialize(scenarioInfo);
            testRunner.ScenarioContext.ScenarioContainer.RegisterInstanceAs<Xunit.Abstractions.ITestOutputHelper>(_testOutputHelper);
        }
        
        public virtual void ScenarioStart()
        {
            testRunner.OnScenarioStart();
        }
        
        public virtual void ScenarioCleanup()
        {
            testRunner.CollectScenarioErrors();
        }
        
        public virtual void FeatureBackground()
        {
#line 21
    #line hidden
#line 22
        testRunner.Given("a DICOM destination registered with Informatics Gateway", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line hidden
        }
        
        void System.IDisposable.Dispose()
        {
            this.TestTearDown();
        }
        
        [Xunit.SkippableTheoryAttribute(DisplayName="Export to a DICOM device")]
        [Xunit.TraitAttribute("FeatureTitle", "DICOM DIMSE SCU Services")]
        [Xunit.TraitAttribute("Description", "Export to a DICOM device")]
        [Xunit.TraitAttribute("Category", "messaging_export_complete")]
        [Xunit.TraitAttribute("Category", "messaging")]
        [Xunit.InlineDataAttribute("MR", "1", new string[0])]
        [Xunit.InlineDataAttribute("CT", "1", new string[0])]
        [Xunit.InlineDataAttribute("MG", "1", new string[0])]
        [Xunit.InlineDataAttribute("US", "1", new string[0])]
        [Xunit.InlineDataAttribute("TOMO", "1", new string[0])]
        [Xunit.InlineDataAttribute("Tiny", "1", new string[0])]
        public virtual void ExportToADICOMDevice(string modality, string count, string[] exampleTags)
        {
            string[] @__tags = new string[] {
                    "messaging_export_complete",
                    "messaging"};
            if ((exampleTags != null))
            {
                @__tags = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Concat(@__tags, exampleTags));
            }
            string[] tagsOfScenario = @__tags;
            System.Collections.Specialized.OrderedDictionary argumentsOfScenario = new System.Collections.Specialized.OrderedDictionary();
            argumentsOfScenario.Add("modality", modality);
            argumentsOfScenario.Add("count", count);
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Export to a DICOM device", null, tagsOfScenario, argumentsOfScenario, this._featureTags);
#line 25
    this.ScenarioInitialize(scenarioInfo);
#line hidden
            bool isScenarioIgnored = default(bool);
            bool isFeatureIgnored = default(bool);
            if ((tagsOfScenario != null))
            {
                isScenarioIgnored = tagsOfScenario.Where(__entry => __entry != null).Where(__entry => String.Equals(__entry, "ignore", StringComparison.CurrentCultureIgnoreCase)).Any();
            }
            if ((this._featureTags != null))
            {
                isFeatureIgnored = this._featureTags.Where(__entry => __entry != null).Where(__entry => String.Equals(__entry, "ignore", StringComparison.CurrentCultureIgnoreCase)).Any();
            }
            if ((isScenarioIgnored || isFeatureIgnored))
            {
                testRunner.SkipScenario();
            }
            else
            {
                this.ScenarioStart();
#line 21
    this.FeatureBackground();
#line hidden
#line 26
        testRunner.Given(string.Format("{0} {1} studies for export", count, modality), ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line hidden
#line 27
        testRunner.When("a export request is sent for \'md.export.request.monaiscu\'", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
#line 28
        testRunner.Then("Informatics Gateway exports the studies to the DICOM SCP", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Then ");
#line hidden
            }
            this.ScenarioCleanup();
        }
        
        [System.CodeDom.Compiler.GeneratedCodeAttribute("TechTalk.SpecFlow", "3.9.0.0")]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
        public class FixtureData : System.IDisposable
        {
            
            public FixtureData()
            {
                DICOMDIMSESCUServicesFeature.FeatureSetup();
            }
            
            void System.IDisposable.Dispose()
            {
                DICOMDIMSESCUServicesFeature.FeatureTearDown();
            }
        }
    }
}
#pragma warning restore
#endregion
