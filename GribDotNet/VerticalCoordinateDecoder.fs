﻿module GribDotNet.VerticalCoordinateDecoder

open ProductDefinitionSection

let tropopauseFeet = 36089.0
let seaLevelPressurePascals = 101325.0
let pascalsToAltitudeMetres pascals = (1.0 - (pascals/101325.0)**(1.0/5.25588))/2.25577e-5
let altitudeMetresToPascals metres = seaLevelPressurePascals*(1.0-metres*2.25577e-5)**5.25588
let metresToFeet metres = 3.28084*metres
let pascalsToAltitudeFeet pascals = pascals |> pascalsToAltitudeMetres |> metresToFeet

// http://ruc.noaa.gov/pdf/RAPbrief.NWS-22Feb2012-FINAL-b.pdf
let sigmaLevels = // Assuming higher sigma = higher level
    [|
        0.0; // Not using level 0
        1.0000; 0.9980; 0.9940; 0.9870; 0.9750; 0.9590; 
        0.9390; 0.9160; 0.8920; 0.8650; 0.8350; 0.8020; 0.7660; 
        0.7270; 0.6850; 0.6400; 0.5920; 0.5420; 0.4970; 0.4565; 
        0.4205; 0.3877; 0.3582; 0.3317; 0.3078; 0.2863; 0.2670; 
        0.2496; 0.2329; 0.2188; 0.2047; 0.1906; 0.1765; 0.1624; 
        0.1483; 0.1342; 0.1201; 0.1060; 0.0919; 0.0778; 0.0657; 
        0.0568; 0.0486; 0.0409; 0.0337; 0.0271; 0.0209; 0.0151; 
        0.0097; 0.0047
    |]

let decodeSigma sigmaLevel =
    let pressurePascals = seaLevelPressurePascals*sigmaLevels.[sigmaLevel]
    let altitudeMetres = pascalsToAltitudeMetres pressurePascals
    let altitudeFeet = metresToFeet altitudeMetres
    altitudeFeet

let decodeVertical (surfaceType:FixedSurfaceType) value =
    match surfaceType with
    | GroundOrWaterSurface -> Choice1Of2(0.0)
    | IsobaricSurface -> Choice1Of2(pascalsToAltitudeFeet value)
    | SigmaLevel -> Choice1Of2(decodeSigma (int value))
    | HybridLevel -> Choice1Of2(decodeSigma (int value)) // Apparently hybrid is actually sigma
    | SpecificAltitudeAboveMeanSeaLevel -> Choice1Of2(metresToFeet value) // Approximation
    | SpecificHeightLevelAboveGround -> Choice1Of2(metresToFeet value) // Approximation
    | LevelAtSpecifiedPressureDifferenceFromGroundToLevel -> Choice1Of2(pascalsToAltitudeFeet (value+seaLevelPressurePascals))
    | Tropopause -> Choice1Of2(tropopauseFeet)
    | _ -> Choice2Of2(surfaceType)