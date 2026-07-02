using System;
using System.IO;
using Newtonsoft.Json.Linq;
using RfpTestStation.Core.Model;
using RfpTestStation.Core.Reporting;
using RfpTestStation.Core.Workflow;
using Xunit;

namespace RfpTestStation.Tests.Reporting
{
    public sealed class ResultWriterTests
    {
        [Fact]
        public void JsonWriterUsesProductionFileNameAndPreservesStepResults()
        {
            using (var temp = new TempDirectory())
            {
                var report = SampleReport(passed: true);
                var writer = new JsonResultWriter();

                var path = writer.Write(temp.Path, report);

                Assert.Equal(Path.Combine(temp.Path, "SN001_20260630_083015_Pass.json"), path);
                var json = JObject.Parse(File.ReadAllText(path));
                Assert.Equal("SN001", (string)json["SerialNumber"]!);
                Assert.Equal("Measure, One", (string)json["StepResults"]![0]!["StepName"]!);
                Assert.Equal("Passed", (string)json["StepResults"]![0]!["Status"]!);
            }
        }

        [Fact]
        public void CsvWriterEscapesValuesAndIncludesExpectedColumns()
        {
            using (var temp = new TempDirectory())
            {
                var report = SampleReport(passed: false);
                var writer = new CsvResultWriter();

                var path = writer.Write(temp.Path, report);

                Assert.Equal(Path.Combine(temp.Path, "SN001_20260630_083015_Fail.csv"), path);
                var csv = File.ReadAllText(path);
                Assert.Contains("StepName,Status,Value,LowLimit,HighLimit,Unit,StartTime,EndTime,Message", csv);
                Assert.Contains("\"Measure, One\",Passed,12.34,10,15,V", csv);
                Assert.Contains("\"contains \"\"quote\"\"\"", csv);
            }
        }

        [Fact]
        public void RunLogWriterWritesReadableRunAndStepTimeline()
        {
            using (var temp = new TempDirectory())
            {
                var report = SampleReport(passed: false);
                report.TestPlanName = "RFP 7000 V2";
                report.TestPlanPath = @"D:\Station\Rfp7000V2.testplan.json";
                report.ConfigPath = @"D:\Station\Config.json";
                report.ExecutionMode = "Hardware";
                report.Operator = "OperatorA";
                report.StepItems.Add(new TestItem("flash.tcon", "TCON Flash", TestItemKind.Flash)
                {
                    SourceReference = "MainSequence: TCON",
                    IsRequired = true,
                    StopOnFailure = true,
                    Timeout = TimeSpan.FromSeconds(600),
                    Parameters =
                    {
                        ["script"] = @"..\Flash\RedCase_Auto\Debug\FlashUpdate_Run.bat",
                        ["flashKind"] = "RedCase"
                    }
                });
                report.StepResults.Clear();
                report.StepResults.Add(new StepResult
                {
                    StepName = "TCON Flash",
                    Status = StepStatus.Error,
                    Value = "HX6330.bin",
                    ExternalLogPath = @"C:\Logs\SN001_tcon_fail.log",
                    StartTime = DateTimeOffset.Parse("2026-06-30T08:30:20+08:00"),
                    EndTime = DateTimeOffset.Parse("2026-06-30T08:30:25+08:00"),
                    Message = "FlashUpdate.exe exited with code 4",
                    Error = new InvalidOperationException("Bin file not found")
                });
                var writer = new RunLogWriter();

                var path = writer.Write(temp.Path, report);

                Assert.Equal(Path.Combine(temp.Path, "SN001_20260630_083015_Fail.log"), path);
                var text = File.ReadAllText(path);
                Assert.Contains("RUN START", text);
                Assert.Contains("SerialNumber: SN001", text);
                Assert.Contains("TestPlan: RFP 7000 V2", text);
                Assert.Contains("Config: D:\\Station\\Config.json", text);
                Assert.Contains("[1] STEP START", text);
                Assert.Contains("Id: flash.tcon", text);
                Assert.Contains("Name: TCON Flash", text);
                Assert.Contains("Kind: Flash", text);
                Assert.Contains("Required: True", text);
                Assert.Contains("StopOnFailure: True", text);
                Assert.Contains("TimeoutSeconds: 600", text);
                Assert.Contains("\"script\": \"..\\\\Flash\\\\RedCase_Auto\\\\Debug\\\\FlashUpdate_Run.bat\"", text);
                Assert.Contains("[1] STEP END", text);
                Assert.Contains("Status: Error", text);
                Assert.Contains("ExternalLogPath: C:\\Logs\\SN001_tcon_fail.log", text);
                Assert.Contains("DurationMs: 5000", text);
                Assert.Contains("Message: FlashUpdate.exe exited with code 4", text);
                Assert.Contains("Exception: System.InvalidOperationException: Bin file not found", text);
                Assert.Contains("RUN END", text);
            }
        }

        private static RunReport SampleReport(bool passed)
        {
            return new RunReport
            {
                SerialNumber = "SN001",
                StartedAt = DateTimeOffset.Parse("2026-06-30T08:30:15+08:00"),
                FinishedAt = DateTimeOffset.Parse("2026-06-30T08:31:15+08:00"),
                Passed = passed,
                StepResults =
                {
                    new StepResult
                    {
                        StepName = "Measure, One",
                        Status = StepStatus.Passed,
                        Value = 12.34,
                        LowLimit = 10,
                        HighLimit = 15,
                        Unit = "V",
                        StartTime = DateTimeOffset.Parse("2026-06-30T08:30:20+08:00"),
                        EndTime = DateTimeOffset.Parse("2026-06-30T08:30:21+08:00"),
                        Message = "contains \"quote\""
                    }
                }
            };
        }

        private sealed class TempDirectory : IDisposable
        {
            public TempDirectory()
            {
                Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "RfpTestStation_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(Path);
            }

            public string Path { get; }

            public void Dispose()
            {
                if (Directory.Exists(Path))
                {
                    Directory.Delete(Path, recursive: true);
                }
            }
        }
    }
}
