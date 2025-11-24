using Asahi.Helpers;
using Asahi.Vision.HalconProcedures;
using Asahi.Vision.Handlers.Core;
using Asahi.Vision.Handlers.Interfaces;
using Asahi.Vision.Results;
using HalconDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asahi.Vision.Handlers.Steps
{
    public class BarcodeInspection: IInspectionStep
    {
        private readonly HTuple? _fillLevelHandle;
        public string Name => "Fill Level Inspection";

        public BarcodeInspection(HTuple? fillLevelHandle = null)
        {
            _fillLevelHandle = fillLevelHandle;
        }

        public Task RunAsync(InspectionContext context)
        {
            return Task.Run(() =>
            {
                if (context.Image == null || !context.Image.IsInitialized())
                {
                    AppLogger.Error($"[{Name}] Image is null or not initialized");

                    context.InspectionResults[Name] = new InspectionResult
                    {
                        InspectionName = Name,
                        Passed = false,
                        Confidence = 0.0,
                        InspectionComplete = true
                    };
                    return;
                }

                try
                {
                    string[] barcodeCharacters = BarcodeDetectionProcedure.GetBarcode(context.Image);

                    context.Parameters["BarCode"] = barcodeCharacters;

                    var result = new InspectionResult
                    {
                        InspectionName = Name,
                        Passed = true,
                        //Confidence = fillLevelPercentage / 100.0,
                        InspectionComplete = true
                    };

                    context.InspectionResults[Name] = result;

                    AppLogger.Info($"[{Name}] Bar Code Characters: {barcodeCharacters}%");
                }
                catch (Exception ex)
                {
                    AppLogger.Error($"[{Name}] Failed to execute BarcodeInspection", ex);

                    // Create a failed result when exception occurs
                    context.InspectionResults[Name] = new InspectionResult
                    {
                        InspectionName = Name,
                        Passed = false,
                        Confidence = 0.0,
                        InspectionComplete = true
                    };
                }
            });
        }
    }
}
