using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Runtime.InteropServices;
using System;
using System.Text;
using System.Diagnostics;

namespace simul
{	
    public class CreateSequence
	{

		public static string DefaultSequence =
        @"
	""cloudKeyframer 9"":
	{
		""CloudClass"": ""0""
		""CloudRegionFadeTexture"": ""CloudRegionFade.png""
		""CloudRegionGradientTexture"": ""CloudRegionGradient.png""
		""CloudRegionMapTexture"": ""CloudRegionMap.png""
		""DayLoop"": ""true""
		""Enabled"": ""true""
		""ExplicitOffsets"": ""false""
		""GridHeight"": ""16""
		""GridWidth"": ""128""
		""IDColour"": ""0.001251258887350559,0.5635853409767151,0.1933042407035828,               1""
		""LoopRange"": ""               1""
		""MapTexture"": """"
		""Name"": ""Cloud Layer""
		""NoisePeriod"": ""               4""
		""NoisePhase"": ""               0""
		""NoiseResolution"": ""8""
		""NumSubdivisions"": ""8""
		""SubdivisionMethod"": ""2""
		""SubdivisionRealTimeInterval"": ""               5""
		""SubdivisionTimeInterval"": ""0.009999999776482582""
		""Viscosity"": ""               0""
		""VorticityConfinement"": ""               0""
		""Wrap"": ""true""
		""branchAngleDegrees"": ""              45""
		""colour.x"": ""               1""
		""colour.y"": ""0.800000011920929""
		""colour.z"": ""0.800000011920929""
		""maxRadiance"": ""            1000""
		""minPixelWidth"": ""             1.5""
		""motion"": ""               0""
		""numBranches"": ""2""
		""numLevels"": ""2""
		""realTime"": ""true""
		""roughness"": ""0.2000000029802322""
		""seed"": ""1""
		""sheet"": ""0.1000000014901161""
		""strikeThicknessMetres"": ""0.02999999932944775""
		""cloudRegions"":
		[
		]
		""keyframes"":
		[
		{
			""EdgeWorleyNoise"": ""0.300000011920929""
			""WorleyNoise"": ""           0.125""
			""WorleyScale"": ""               3""
			""baseNoiseFactor"": ""            0.75""
			""churn"": ""               1""
			""cloud_base_km"": ""               3""
			""cloud_height_km"": ""               8""
			""cloud_width_km"": ""             120""
			""cloudiness"": ""0.6000000238418579""
			""diffusivity"": ""0.449999988079071""
			""distributionBaseLayer"": ""0.1299999952316284""
			""distributionTransition"": ""0.2000000029802322""
			""edgeSharpness"": ""0.05999999865889549""
			""fractal_amplitude"": ""               2""
			""maxDensityGm3"": ""             0.5""
			""octaves"": ""5""
			""offsetKmx"": ""               0""
			""offsetKmy"": ""               0""
			""origin"": ""             0.5,             0.5,             0.5,             0.5""
			""persistence"": ""0.1000000014901161""
			""precipitation"": ""               0""
			""precipitationRegion.edge"": ""0.1000000014901161""
			""precipitationRegion.lockToClouds"": ""true""
			""precipitationRegion.rainCentreS"": ""               1""
			""precipitationRegion.rainCentreX"": ""               0""
			""precipitationRegion.rainCentreY"": ""               0""
			""precipitationRegion.rainCentreZ"": ""               0""
			""precipitationRegion.rainRadiusKm"": ""              50""
			""precipitationRegion.regional"": ""false""
			""precipitationRegion.virgaStrength"": ""               0""
			""precipitation_base_km"": ""               0""
			""rain_to_snow"": ""               0""
			""scalekm"": ""             200,             200,              10""
			""simulation"": ""               0""
			""stormRegion.base_radial_km"": ""            6378""
			""stormRegion.lockToClouds"": ""true""
			""stormRegion.rainCentreS"": ""               1""
			""stormRegion.rainCentreX"": ""               0""
			""stormRegion.rainCentreY"": ""               0""
			""stormRegion.rainCentreZ"": ""               0""
			""stormRegion.rainRadiusKm"": ""              50""
			""strikeDurationSeconds"": ""             0.5""
			""strikeFrequencyPerSecond"": ""               0""
			""time"": ""0.5983555316925049""
			""uid"": ""4""
			""upperDensity"": ""0.8500000238418579""
			""cloudRegionStates"":
			[
			]
		}
		]
	}
	""skyKeyframer"":
	{
		""BackgroundTexture"": ""MilkyWayEdited.png""
		""BaseOzone"": ""0.05999999865889549,0.03999999910593033,0.009999999776482582,               0""
		""BrightnessPower"": ""            0.25""
		""ColourWavelengthsNm"": ""             630,             540,             450,               0""
		""DayLoop"": ""true""
		""Emissivity"": ""               0""
		""LinkKeyframeTimeAndDaytime"": ""true""
		""LoopRange"": ""               1""
		""MaxAltitudeKm"": ""              15""
		""MaxDistanceKm"": ""             500""
		""MaxStarMagnitude"": ""               6""
		""MaxSunRadiance"": ""            5000""
		""NumAltitudes"": ""4""
		""NumDistances"": ""32""
		""NumElevations"": ""32""
		""NumSubdivisions"": ""8""
		""OzoneStrength"": ""0.05000000074505806""
		""Rayleigh"": ""0.007125694304704666,0.01320122834295034,0.02737406641244888""
		""StartDayNumber"": ""0""
		""SubdivisionMethod"": ""1""
		""SubdivisionRealTimeInterval"": ""               5""
		""SubdivisionTimeInterval"": ""0.009999999776482582""
		""SunIrradiance"": ""              25,              25,              25""
		""SunRadiusArcMinutes"": ""              16""
		""SunTexture"": ""Sun.png""
		""Time"": ""-0.4822833836078644""
		""atmosphereThicknessKm"": ""             100""
		""planetRadiusKm"": ""            6378""
		""aurora"":
		{
			""AuroraElectronFreeTime"": ""9.999999960041972e-13""
			""AuroraElectronVolumeDensity"": ""   9999999827968""
			""AuroraIntensityMapSize"": ""512""
			""AuroraTraceLength"": ""100""
			""AuroralLayersIntensity"": ""               1""
			""End_Dawn1"": ""             -30""
			""End_Dawn2"": ""               0""
			""End_Dusk1"": ""            -180""
			""End_Dusk2"": ""            -150""
			""GeomagneticNorthPole"": ""0.08057441974941291,0.01227205601279777,0.1500697400752456,0.9853102215226495""
			""HighestLatitude"": ""              80""
			""LowestLatitude"": ""              60""
			""MaxBand"": ""              10""
			""MinBand"": ""               3""
			""OriginLatitude_Dawn1"": ""              90""
			""OriginLatitude_Dawn2"": ""              90""
			""OriginLatitude_Dusk1"": ""              90""
			""OriginLatitude_Dusk2"": ""              90""
			""OriginLongitude_Dawn1"": ""               0""
			""OriginLongitude_Dawn2"": ""               0""
			""OriginLongitude_Dusk1"": ""               0""
			""OriginLongitude_Dusk2"": ""               0""
			""Radius_Dawn1"": ""              19""
			""Radius_Dawn2"": ""              21""
			""Radius_Dusk1"": ""              20""
			""Radius_Dusk2"": ""              22""
			""Start_Dawn1"": ""             180""
			""Start_Dawn2"": ""             150""
			""Start_Dusk1"": ""               0""
			""Start_Dusk2"": ""              30""
			""AuroraLayers"":
			[
			{
				""Base"": ""              85""
				""EmittedWavelength"": ""427.7999877929688""
				""Strength"": ""              30""
				""Top"": ""             105""
			}
			{
				""Base"": ""              85""
				""EmittedWavelength"": ""             670""
				""Strength"": ""              18""
				""Top"": ""             105""
			}
			{
				""Base"": ""              85""
				""EmittedWavelength"": ""391.3999938964844""
				""Strength"": ""               6""
				""Top"": ""             105""
			}
			{
				""Base"": ""             100""
				""EmittedWavelength"": ""557.7000122070312""
				""Strength"": ""             100""
				""Top"": ""             210""
			}
			{
				""Base"": ""             100""
				""EmittedWavelength"": ""             471""
				""Strength"": ""               6""
				""Top"": ""             210""
			}
			{
				""Base"": ""             200""
				""EmittedWavelength"": ""             630""
				""Strength"": ""              30""
				""Top"": ""             250""
			}
			]
		}
		""keyframes"":
		[
		{
			""AutoMie"": ""true""
			""CustomSkyColours"": ""false""
			""FogCeilingKm"": ""               1""
			""GroundFog"": ""               0""
			""GroundFogColour"": ""               1,               1,               1""
			""Haze"": ""               1""
			""HazeBaseKm"": ""               0""
			""HazeEccentricity"": ""0.8199999928474426""
			""HazeScaleKm"": ""             0.5""
			""Horizon"": ""0.5099999904632568,0.8700000047683716,               1""
			""Mie"": ""0.006106850225478411,0.00817577913403511,0.01170013006776571,               0""
			""SeaLevelTemperatureK"": ""288.1499938964844""
			""Zenith"": ""0.05999999865889549,0.4699999988079071,               1""
			""automaticSunPosition"": ""true""
			""daytime"": ""0.5615965723991394""
			""time"": ""0.5615965723991394""
			""uid"": ""10""
		}
		]
	}
@";






        [MenuItem("Assets/Create/trueSKY Sequence", false, 1000)]
		public static void CreateSequenceAsset()
		{
			// DirectoryCopy.CopyPluginsAndGizmosToAssetsFolder();
			Sequence asset = CustomAssetUtility.CreateAsset<Sequence>();
			Selection.activeObject = asset;
			//A default sequence with some clouds
			asset.SequenceAsText = DefaultSequence;


		}
	}
}