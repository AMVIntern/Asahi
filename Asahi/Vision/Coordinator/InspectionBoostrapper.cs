using Asahi.AppCycleManager;
using Asahi.DataServices;
using Asahi.Stores;
using Asahi.ViewModels;
using Asahi.Vision.HalconProcedures;
using Asahi.Vision.Handlers.Interfaces;
using Asahi.Vision.Handlers.Steps;
using Asahi.Vision.Runners;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asahi.Vision.Coordinator
{
    public class InspectionBoostrapper
    {
        private readonly TriggerSessionManager _triggerSessionManager;
        private readonly RecipeManagerViewModel _recipeManagerViewModel;

        public InspectionCoordinator Coordinator { get; }

        public InspectionBoostrapper(MultiCameraImageStore imageStore, Dictionary<string, CameraViewModel> cameraViewModels, ImageLogger imageLogger, TriggerSessionManager triggerSessionManager, RecipeManagerViewModel recipeManagerViewModel, HomeViewModel? homeViewModel = null)
        {
            _triggerSessionManager = triggerSessionManager;
            _recipeManagerViewModel = recipeManagerViewModel;

            var runners = new Dictionary<string, IInspectionRunner>()
            {
                {
                    "Cam1", new SequentialInspectionRunner(new IInspectionStep[]
                    {
                        new BarcodeInspection()
                    })
                },
            };

            Coordinator = new InspectionCoordinator(runners, imageStore, cameraViewModels, imageLogger, _triggerSessionManager, homeViewModel);
        }
    }
}
