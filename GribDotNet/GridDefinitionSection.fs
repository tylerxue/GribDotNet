﻿module GribDotNet.GridDefinitionSection

open LambertConverter

type LambertConformalTemplate = {
    ShapeOfEarth: byte;
    ScaleFactorOfRadiusOfSphericalEarth: byte;
    ScaleValueOfRadiusOfSphericalEarth: uint32;
    ScaleFactorOfMajorAxisOfOblateSpheroidEarth: byte;
    ScaleValueOfMajorAxisOfOblateSpheroidEarth: uint32;
    ScaleFactorOfMinorAxisOfOblateSpheroidEarth: byte;
    ScaleValueOfMinorAxisOfOblateSpheroidEarth: uint32;
    NumberOfPointsOnXAxis: uint32;
    NumberOfPointsOnYAxis: uint32;
    LatitudeOfFirstGridPoint: uint32;
    LongitudeOfFirstGridPoint: uint32;
    ResolutionAndComponentFlags: byte;
    LatitudeWhereDxAndDyAreSpecified: uint32;
    LongitudeOfMeridianParallelToYAxisAlongWhichLatitudeIncreasesAsTheYCoordinateIncreases: uint32;
    XDirectionGridLength: uint32;
    YDirectionGridLength: uint32;
    ProjectionCenterFlag: byte;
    ScanningMode: byte;
    FirstLatitudeFromThePoleAtWhichTheSecantConeCutsTheSphere: uint32;
    SecondLatitudeFromThePoleAtWhichTheSecantConeCutsTheSphere: uint32;
    LatitudeOfSouthernPoleOfProjection: uint32;
    LongitudeOfSouthernPoleOfProjection: uint32;
}

let makeLambertProjection1 (template:LambertConformalTemplate) =
    if template.FirstLatitudeFromThePoleAtWhichTheSecantConeCutsTheSphere <>
        template.SecondLatitudeFromThePoleAtWhichTheSecantConeCutsTheSphere then
        failwith "Not single reference parallel template!" // Call the secant decoder instead
    if template.FirstLatitudeFromThePoleAtWhichTheSecantConeCutsTheSphere <>
        template.LatitudeWhereDxAndDyAreSpecified then
        failwith "Grid increments not specified at tangent latitude!" // Would require further decoding
    if template.ShapeOfEarth <> 6uy then
        failwith "Unexpected Earth shape used!" // Would require further decoding
    if template.ScanningMode <> 64uy then
        failwith "Unexpected scanning mode used!" // Would require algorithmic changes
    if template.ResolutionAndComponentFlags <> 8uy then
        failwith "Unexpected resolution/component flags used!" // Would require algorithmic changes
    let projection = {
        StandardParallel0 = float template.FirstLatitudeFromThePoleAtWhichTheSecantConeCutsTheSphere * 1e-6<Latitude>
        ReferenceLatitude = float template.LatitudeWhereDxAndDyAreSpecified * 1e-6<Latitude>
        ReferenceLongitude = float template.LongitudeOfMeridianParallelToYAxisAlongWhichLatitudeIncreasesAsTheYCoordinateIncreases * 1e-6<Longitude>
        EarthRadius = 6371229.0 // in metres
    }
    projection

let getOriginLatitudeLongitude (template:LambertConformalTemplate) =
    (float template.LatitudeOfFirstGridPoint*1e-6<Latitude>, float template.LongitudeOfFirstGridPoint*1e-6<Longitude>)

// Triple of origin, increments, points note that this is in metres
let makeGridDefinition1 (template:LambertConformalTemplate) (projection:LambertConverter.LambertProjection1) =
    let height = int template.NumberOfPointsOnYAxis
    let width = int template.NumberOfPointsOnXAxis
    let (originX, originY) as origin =
        toLambert1 projection (getOriginLatitudeLongitude template)
    let eastIncrement = float template.XDirectionGridLength * 1e-3 // Decode from milimetres
    let northIncrement = float template.YDirectionGridLength * 1e-3 // Decode from milimetres
    (origin, (eastIncrement,northIncrement), (width,height))
    (*
    let metresToNauticalMiles = 0.000539957
    let originNm = (originX*metresToNauticalMiles,originY*metresToNauticalMiles)
    let eastNauticalMiles = eastIncrement * metresToNauticalMiles
    let northNauticalMiles = northIncrement * metresToNauticalMiles
    (originNm, (eastNauticalMiles,northNauticalMiles), (width,height))
    *)

let decodeLatitudeLongitude (template:LambertConformalTemplate) =
    let projection = makeLambertProjection1 template
    let ((originEast,originNorth),(eastIncrement,northIncrement),(width,height)) =
        makeGridDefinition1 template projection
    let result = Array.init height (fun _ -> Array.create width (0.0<Latitude>, 0.0<Longitude>)) // height x width jagged array
    for j = 0 to height - 1 do
        let northing = float j * northIncrement + originNorth
        for i = 0 to width - 1 do
            let easting = float i * eastIncrement + originEast
            let latitudeLongitude = fromLambert1 projection (easting,northing)
            result.[j].[i] <- latitudeLongitude
    result

type GridDefinitionTemplate =
    | LambertConformal of LambertConformalTemplate
    | Other of uint16 * byte[]

let asLambertComformalTemplate gridTemplate =
    match gridTemplate with
    | LambertConformal x -> x
    | _ -> failwith "Not Lambert Conformal Template!"

let readLambertConformalTemplate (reader:System.IO.BinaryReader) = {
    ShapeOfEarth = reader.ReadByte();
    ScaleFactorOfRadiusOfSphericalEarth = reader.ReadByte();
    ScaleValueOfRadiusOfSphericalEarth = System.BitConverter.ToUInt32(Array.rev(reader.ReadBytes(4)), 0);
    ScaleFactorOfMajorAxisOfOblateSpheroidEarth = reader.ReadByte();
    ScaleValueOfMajorAxisOfOblateSpheroidEarth = System.BitConverter.ToUInt32(Array.rev(reader.ReadBytes(4)), 0);
    ScaleFactorOfMinorAxisOfOblateSpheroidEarth = reader.ReadByte();
    ScaleValueOfMinorAxisOfOblateSpheroidEarth = System.BitConverter.ToUInt32(Array.rev(reader.ReadBytes(4)), 0);
    NumberOfPointsOnXAxis = System.BitConverter.ToUInt32(Array.rev(reader.ReadBytes(4)), 0);
    NumberOfPointsOnYAxis = System.BitConverter.ToUInt32(Array.rev(reader.ReadBytes(4)), 0);
    LatitudeOfFirstGridPoint = System.BitConverter.ToUInt32(Array.rev(reader.ReadBytes(4)), 0);
    LongitudeOfFirstGridPoint = System.BitConverter.ToUInt32(Array.rev(reader.ReadBytes(4)), 0);
    ResolutionAndComponentFlags = reader.ReadByte();
    LatitudeWhereDxAndDyAreSpecified = System.BitConverter.ToUInt32(Array.rev(reader.ReadBytes(4)), 0);
    LongitudeOfMeridianParallelToYAxisAlongWhichLatitudeIncreasesAsTheYCoordinateIncreases = System.BitConverter.ToUInt32(Array.rev(reader.ReadBytes(4)), 0);
    XDirectionGridLength = System.BitConverter.ToUInt32(Array.rev(reader.ReadBytes(4)), 0);
    YDirectionGridLength = System.BitConverter.ToUInt32(Array.rev(reader.ReadBytes(4)), 0);
    ProjectionCenterFlag = reader.ReadByte();
    ScanningMode = reader.ReadByte();
    FirstLatitudeFromThePoleAtWhichTheSecantConeCutsTheSphere = System.BitConverter.ToUInt32(Array.rev(reader.ReadBytes(4)), 0);
    SecondLatitudeFromThePoleAtWhichTheSecantConeCutsTheSphere = System.BitConverter.ToUInt32(Array.rev(reader.ReadBytes(4)), 0);
    LatitudeOfSouthernPoleOfProjection = System.BitConverter.ToUInt32(Array.rev(reader.ReadBytes(4)), 0);
    LongitudeOfSouthernPoleOfProjection = System.BitConverter.ToUInt32(Array.rev(reader.ReadBytes(4)), 0);
}

let readGridDefinitionTemplate (reader:System.IO.BinaryReader) templateNumber templateLength = 
    match templateNumber with
    | 30us ->
        let template = readLambertConformalTemplate reader
        LambertConformal template
    | _ -> Other (templateNumber, (reader.ReadBytes(templateLength)))

type GridDefinitionSection = {
    SectionLength: uint32;
    SourceOfGridDefinition: byte;
    NumberOfDataPoints: uint32;
    NumberOfOctetsForOptionalListOfNumbersDefiningNumberOfPoints: byte;
    InterpolationOfListOfNumbersDefiningNumberOfPoints: byte;
    GridDefinitionTemplate: GridDefinitionTemplate;
    OptionalListOfNumbersDefiningNumberOfPoints: byte[];
}

let readGridDefinitionSection (reader:System.IO.BinaryReader) sectionLength =
    let sourceOfGridDefinition = reader.ReadByte()
    let numberOfDataPoints = System.BitConverter.ToUInt32(Array.rev(reader.ReadBytes(4)), 0)
    let numberOfOctetsForOptionalListOfNumbersDefiningNumberOfPoints = reader.ReadByte()
    let interpolationOfListOfNumbersDefiningNumberOfPoints = reader.ReadByte()
    
    let gridDefinitionTemplateNumber = System.BitConverter.ToUInt16(Array.rev(reader.ReadBytes(2)), 0)
    let templateLength = (int) sectionLength - 14 - (int) numberOfOctetsForOptionalListOfNumbersDefiningNumberOfPoints;
    let gridDefinitionTemplate = readGridDefinitionTemplate reader gridDefinitionTemplateNumber templateLength
    let optionalListOfNumbersDefiningNumberOfPoints = reader.ReadBytes((int) numberOfOctetsForOptionalListOfNumbersDefiningNumberOfPoints)

    {
        SectionLength = sectionLength;
        SourceOfGridDefinition = sourceOfGridDefinition;
        NumberOfDataPoints = numberOfDataPoints;
        NumberOfOctetsForOptionalListOfNumbersDefiningNumberOfPoints = numberOfOctetsForOptionalListOfNumbersDefiningNumberOfPoints;
        InterpolationOfListOfNumbersDefiningNumberOfPoints = interpolationOfListOfNumbersDefiningNumberOfPoints;
        GridDefinitionTemplate = gridDefinitionTemplate;
        OptionalListOfNumbersDefiningNumberOfPoints = optionalListOfNumbersDefiningNumberOfPoints;
    }
