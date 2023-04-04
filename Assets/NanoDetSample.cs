/* 
*   NanoDet
*   Copyright © 2023 NatML Inc. All Rights Reserved.
*/

namespace NatML.Examples {

    using UnityEngine;
    using NatML.VideoKit;
    using NatML.Vision;
    using Visualizers;

    public sealed class NanoDetSample : MonoBehaviour {

        [Header(@"VideoKit")]
        public VideoKitCameraManager cameraManager;

        [Header(@"UI")]
        public NanoDetVisualizer visualizer;

        private MLModelData modelData;
        private MLModel model;
        private NanoDetPredictor predictor;

        private async void Start () {
            // Create the NanoDet predictor
            predictor = await NanoDetPredictor.Create();
            // Listen for camera frames
            cameraManager.OnCameraFrame.AddListener(OnCameraFrame);
        }

        private void OnCameraFrame (CameraFrame frame) {
            // Detect
            var detections = predictor.Predict(frame);
            // Visualize
            visualizer.Render(detections);
        }

        void OnDisable () {
            // Stop listening for camera frames
            cameraManager.OnCameraFrame.RemoveListener(OnCameraFrame);
            // Dispose the model
            model?.Dispose();
        }
    }
}