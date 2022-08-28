/* 
*   NanoDet
*   Copyright (c) 2022 NatML Inc. All Rights Reserved.
*/

namespace NatML.Visualizers {

    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// </summary>
    [RequireComponent(typeof(RawImage), typeof(AspectRatioFitter))]
    public sealed class NanoDetVisualizer : MonoBehaviour {

        #region --Client API--
        /// <summary>
        /// Render a set of object detections.
        /// </summary>
        /// <param name="image">Image which detections are made on.</param>
        /// <param name="detections">Detections to render.</param>
        public void Render (Texture image, params (Rect rect, string label, float score)[] detections) {
            // Delete current
            foreach (var rect in currentRects)
                GameObject.Destroy(rect.gameObject);
            currentRects.Clear();
            // Display image
            var rawImage = GetComponent<RawImage>();
            var aspectFitter = GetComponent<AspectRatioFitter>();
            rawImage.texture = image;
            aspectFitter.aspectRatio = (float)image.width / image.height;
            // Render rects
            var imageRect = new Rect(0, 0, image.width, image.height);
            foreach (var detection in detections) {
                var rect = Instantiate(detectionPrefab, transform);
                rect.gameObject.SetActive(true);
                rect.Render(rawImage, detection.rect, detection.label, detection.score);
                currentRects.Add(rect);
            }
        }
        #endregion


        #region --Operations--
        [SerializeField] NanoDetDetection detectionPrefab;
        List<NanoDetDetection> currentRects = new List<NanoDetDetection>();
        #endregion
    }
}