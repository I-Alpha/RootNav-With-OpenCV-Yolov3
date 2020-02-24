using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RootNav.Data;

namespace RootNav.Core.Measurement
{
    static class RootFormatConverter
    {

        public static SceneMetadata RootNavDataToRSMLMetadata(SceneMetadata.ImageInfo imageInfo, RootNav.IO.TiffHeaderInfo headerInfo, string tag, RootCollection collection)
        {
            SceneMetadata metadata = new SceneMetadata();

            metadata.Image = new SceneMetadata.ImageInfo() { Background = "dark", Hash = imageInfo.Hash, Label = System.IO.Path.GetFileName(imageInfo.Label) };

            metadata.Key = tag;
            metadata.LastModified = DateTime.Now;
            metadata.Resolution = imageInfo.Unit == "pixels" ? 1.0 : imageInfo.Resolution;
            metadata.Unit = imageInfo.Unit == "" || imageInfo.Unit == "pixels" ? "pixel" : imageInfo.Unit;
            metadata.Software = "RootNav";
            metadata.Version = "1";
            metadata.User = System.Environment.UserName;

            return metadata;
        }

        public static SceneInfo RootCollectionToRSMLScene(RootCollection collection)
        {
            SceneInfo scene = new SceneInfo() { Plants = new List<PlantInfo>() };

            foreach (RootBase rootBase in collection.RootTree)
            {   
                // Each rootbase tree represents a plant in this image.
                if (rootBase is RootGroup)
                {
                    PlantInfo plant = new PlantInfo() { Roots = new List<RootInfo>(), RelativeID = rootBase.RelativeID };
                    if (rootBase.Label != "")
                    {
                        plant.Label = rootBase.Label;
                    }

                    RootGroup group = rootBase as RootGroup;

                    // For each primary root
                    foreach (PrimaryRoot primary in group.Children)
                    {
                        RootInfo primaryRootInfo = new RootInfo()
                        {
                            Children = new List<RootInfo>(),
                            RelativeID = primary.RelativeID,
                            RsmlID = primary.RelativeID,
                            Polyline = null,
                            Spline = primary.Spline
                        };

                        if (primary.Label != "")
                        {
                            primaryRootInfo.Label = primary.Label;
                        }
                        
                        foreach (LateralRoot lateral in primary.Children)
                        {
                            RootInfo lateralRootInfo = new RootInfo()
                            {
                                Children = new List<RootInfo>(),
                                RelativeID = lateral.RelativeID,
                                RsmlID = lateral.RelativeID,
                                Polyline = null,
                                Spline = lateral.Spline
                            };

                            if (lateral.Label != "")
                            {
                                lateralRootInfo.Label = lateral.Label;
                            }

                            primaryRootInfo.Children.Add(lateralRootInfo);
                        }

                        plant.Roots.Add(primaryRootInfo);
                    }
                    
                    scene.Plants.Add(plant);
                }
            }

            return scene;
        }

        public static void SetIncompletePropertyOnScene(SceneMetadata metadata, SceneInfo sceneInfo)
        {
            // Metadata
            if (metadata.PropertyDefinitions == null)
            {
                metadata.PropertyDefinitions = new List<PropertyDefinition>();
            }

            metadata.PropertyDefinitions.Add(new PropertyDefinition("complete", PropertyDefinition.PropertyType.Boolean, true));

            // Scene
            if (sceneInfo.Properties == null)
            {
                sceneInfo.Properties = new List<Property>();
            }

            sceneInfo.Properties.Add(new Property("complete", "false"));
        }
    }
}
