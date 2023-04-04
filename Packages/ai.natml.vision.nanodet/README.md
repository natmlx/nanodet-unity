# NanoDet
[NanoDet](https://github.com/RangiLyu/nanodet) high performance general object detection.

## Installing NanoDet
Add the following items to your Unity project's `Packages/manifest.json`:
```json
{
  "scopedRegistries": [
    {
      "name": "NatML",
      "url": "https://registry.npmjs.com",
      "scopes": ["ai.natml"]
    }
  ],
  "dependencies": {
    "ai.natml.vision.nanodet": "1.0.1"
  }
}
```


## Detecting Objects in an Image
First, create the NanoDet predictor:
```csharp
// Create the NanoDet predictor
var predictor = await NanoDetPredictor.Create();
```

Then detect objects in the image:
```csharp
// Create image feature
Texture2D image = ...;
// Detect objects
NanoDetPredictor.Detection[] detections = predictor.Predict(image);
```
___

## Requirements
- Unity 2021.2+

## Quick Tips
- Join the [NatML community on Discord](https://natml.ai/community).
- Discover more ML models on [NatML Hub](https://hub.natml.ai).
- See the [NatML documentation](https://docs.natml.ai/unity).
- Contact us at [hi@natml.ai](mailto:hi@natml.ai).

Thank you very much!