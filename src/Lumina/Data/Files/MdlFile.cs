using System;
using Lumina.Data.Parsing;
using Lumina.Data.Structs;
using Lumina.Extensions;

namespace Lumina.Data.Files {
    public class MdlFile : FileResource {
        public MdlStructs.ModelFileHeader FileHeader;
        public MdlStructs.VertexDeclarationStruct[] VertexDeclarations;
        public MdlStructs.ModelHeader ModelHeader;
        public MdlStructs.ElementIdStruct[] ElementIds;
        public MdlStructs.LodStruct[] Lods;
        public MdlStructs.ExtraLodStruct[] ExtraLods;
        public MdlStructs.MeshStruct[] Meshes;
        public uint[] AttributeNameOffsets;
        public MdlStructs.SubmeshStruct[] Submeshes;

        public MdlStructs.TerrainShadowMeshStruct[] TerrainShadowMeshes;
        public MdlStructs.TerrainShadowSubmeshStruct[] TerrainShadowSubmeshes;

        public uint[] MaterialNameOffsets;
        public uint[] BoneNameOffsets;
        public MdlStructs.BoneTableStruct[] BoneTables;
        public MdlStructs.ShapeStruct[] Shapes;
        public MdlStructs.ShapeMeshStruct[] ShapeMeshes;
        public MdlStructs.ShapeValueStruct[] ShapeValues;

        public ushort[] SubmeshBoneMap;
        public MdlStructs.BoundingBoxStruct BoundingBoxes;
        public MdlStructs.BoundingBoxStruct ModelBoundingBoxes;
        public MdlStructs.BoundingBoxStruct WaterBoundingBoxes;
        public MdlStructs.BoundingBoxStruct VerticalFogBoundingBoxes;
        public MdlStructs.BoundingBoxStruct[] BoneBoundingBoxes;

        public ushort StringCount;
        public byte[] Strings;

        public override void LoadFile()
        {
            bool isLittleEndian = Reader.IsLittleEndian;
            Reader.IsLittleEndian = BitConverter.IsLittleEndian;

            FileHeader = MdlStructs.ModelFileHeader.Read( Reader );
            Reader.IsLittleEndian = isLittleEndian;

            VertexDeclarations = new MdlStructs.VertexDeclarationStruct[FileHeader.VertexDeclarationCount];
            for( int i = 0; i < FileHeader.VertexDeclarationCount; i++ ) VertexDeclarations[ i ] = MdlStructs.VertexDeclarationStruct.Read( Reader );

            StringCount = Reader.ReadUInt16();
            Reader.ReadUInt16();
            uint stringSize = Reader.ReadUInt32();
            Strings = Reader.ReadBytes( (int) stringSize );

            ModelHeader = Reader.ReadStructure<MdlStructs.ModelHeader>();
            ElementIds = new MdlStructs.ElementIdStruct[ModelHeader.ElementIdCount];
            Meshes = new MdlStructs.MeshStruct[ModelHeader.MeshCount];
            BoneTables = new MdlStructs.BoneTableStruct[ModelHeader.BoneTableCount];
            Shapes = new MdlStructs.ShapeStruct[ModelHeader.ShapeCount];
            BoneBoundingBoxes = new MdlStructs.BoundingBoxStruct[ModelHeader.BoneCount];

            for( int i = 0; i < ModelHeader.ElementIdCount; i++ ) ElementIds[ i ] = MdlStructs.ElementIdStruct.Read( Reader );
            Lods = Reader.ReadStructuresAsArray< MdlStructs.LodStruct >( 3 );

            if( ModelHeader.ExtraLodEnabled )
                ExtraLods = Reader.ReadStructuresAsArray< MdlStructs.ExtraLodStruct >( 3 );

            for( int i = 0; i < ModelHeader.MeshCount; i++ ) Meshes[ i ] = MdlStructs.MeshStruct.Read( Reader );
            AttributeNameOffsets = Reader.ReadUInt32Array( ModelHeader.AttributeCount );
            TerrainShadowMeshes = Reader.ReadStructuresAsArray< MdlStructs.TerrainShadowMeshStruct >( ModelHeader.TerrainShadowMeshCount );
            Submeshes = Reader.ReadStructuresAsArray< MdlStructs.SubmeshStruct >( ModelHeader.SubmeshCount );
            TerrainShadowSubmeshes = Reader.ReadStructuresAsArray< MdlStructs.TerrainShadowSubmeshStruct >( ModelHeader.TerrainShadowSubmeshCount );

            MaterialNameOffsets = Reader.ReadUInt32Array( ModelHeader.MaterialCount );
            BoneNameOffsets = Reader.ReadUInt32Array( ModelHeader.BoneCount );
            for( int i = 0; i < ModelHeader.BoneTableCount; i++ ) BoneTables[ i ] = MdlStructs.BoneTableStruct.Read( Reader );

            for( int i = 0; i < ModelHeader.ShapeCount; i++ ) Shapes[ i ] = MdlStructs.ShapeStruct.Read( Reader );
            ShapeMeshes = Reader.ReadStructuresAsArray< MdlStructs.ShapeMeshStruct >( ModelHeader.ShapeMeshCount );
            ShapeValues = Reader.ReadStructuresAsArray< MdlStructs.ShapeValueStruct >( ModelHeader.ShapeValueCount );

            uint submeshBoneMapSize = Reader.ReadUInt32();
            SubmeshBoneMap = Reader.ReadUInt16Array( (int) submeshBoneMapSize / 2 );

            byte paddingAmount = Reader.ReadByte();
            Reader.Seek( Reader.BaseStream.Position + paddingAmount );

            // Dunno what this first one is for?
            BoundingBoxes = MdlStructs.BoundingBoxStruct.Read( Reader );
            ModelBoundingBoxes = MdlStructs.BoundingBoxStruct.Read( Reader );
            WaterBoundingBoxes = MdlStructs.BoundingBoxStruct.Read( Reader );
            VerticalFogBoundingBoxes = MdlStructs.BoundingBoxStruct.Read( Reader );
            for( int i = 0; i < ModelHeader.BoneCount; i++ ) BoneBoundingBoxes[ i ] = MdlStructs.BoundingBoxStruct.Read( Reader );
        }
    }
}