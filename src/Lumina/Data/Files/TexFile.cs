using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Lumina.Data.Attributes;
using Lumina.Data.Parsing.Tex.Buffers;

// ReSharper disable InconsistentNaming

namespace Lumina.Data.Files
{
    [FileExtension( ".tex" )]
    public class TexFile : FileResource
    {
        [Flags]
        public enum Attribute : uint
        {
            DiscardPerFrame = 0x1,
            DiscardPerMap = 0x2,
            Managed = 0x4,
            UserManaged = 0x8,
            CpuRead = 0x10,
            LocationMain = 0x20,
            NoGpuRead = 0x40,
            AlignedSize = 0x80,
            EdgeCulling = 0x100,
            LocationOnion = 0x200,
            ReadWrite = 0x400,
            Immutable = 0x800,
            TextureRenderTarget = 0x100000,
            TextureDepthStencil = 0x200000,
            TextureType1D = 0x400000,
            TextureType2D = 0x800000,
            TextureType3D = 0x1000000,
            TextureTypeCube = 0x2000000,
            TextureTypeMask = 0x3C00000,
            TextureSwizzle = 0x4000000,
            TextureNoTiled = 0x8000000,
            TextureNoSwizzle = 0x80000000,
        }

        /// <summary>
        /// Texture formats. Channel ordering in name follows the enumeration in DXGI_FORMAT.
        ///
        /// Excerpt from: https://docs.microsoft.com/en-us/windows/win32/api/dxgiformat/ne-dxgiformat-dxgi_format
        /// > Most formats have byte-aligned components, and the components are in C-array order (the least address comes first).
        /// > For those formats that don't have power-of-2-aligned components, the first named component is in the least-significant bits.  
        /// </summary>
        [Flags]
        public enum TextureFormat
        {
            TypeShift = 0xC,
            TypeMask = 0xF000,
            ComponentShift = 0x8,
            ComponentMask = 0xF00,
            BppShift = 0x4,
            BppMask = 0xF0,
            EnumShift = 0x0,
            EnumMask = 0xF,
            TypeInteger = 0x1,
            TypeFloat = 0x2,
            TypeDxt = 0x3,
            TypeBc123 = 0x3,
            TypeDepthStencil = 0x4,
            TypeSpecial = 0x5,
            TypeBc57 = 0x6,
            
            Unknown = 0x0,
            
            // Integer types
            L8 = 0x1130,
            A8 = 0x1131,
            B4G4R4A4 = 0x1440,
            B5G5R5A1 = 0x1441,
            B8G8R8A8 = 0x1450,
            B8G8R8X8 = 0x1451,
            
            [Obsolete("Use B4G4R4A4 instead.")]
            R4G4B4A4 = 0x1440,
            [Obsolete("Use B5G5R5A1 instead.")]
            R5G5B5A1 = 0x1441,
            [Obsolete("Use B8G8R8A8 instead.")]
            A8R8G8B8 = 0x1450,
            [Obsolete("Use B8G8R8X8 instead.")]
            R8G8B8X8 = 0x1451,
            [Obsolete("Not supported by Windows DirectX 11 version of the game, nor have any mention of the value, as of 6.15.")]
            A8R8G8B82 = 0x1452,
            
            // Floating point types
            R32F = 0x2150,
            R16G16F = 0x2250,
            R32G32F = 0x2260,
            R16G16B16A16F = 0x2460,
            R32G32B32A32F = 0x2470,
            
            // Block compression types (DX9 names)
            DXT1 = 0x3420,
            DXT3 = 0x3430,
            DXT5 = 0x3431,
            ATI2 = 0x6230,
            
            // Block compression types (DX11 names)
            BC1 = 0x3420,
            BC2 = 0x3430,
            BC3 = 0x3431,
            BC5 = 0x6230,
            BC7 = 0x6432,

            // Depth stencil types
            // Does not exist in ffxiv_dx11.exe: RGBA8 0x4401
            D16 = 0x4140,
            D24S8 = 0x4250,

            // Special types
            Null = 0x5100,
            Shadow16 = 0x5140,
            Shadow24 = 0x5150,
        }

        [StructLayout( LayoutKind.Sequential )]
        public unsafe struct TexHeader
        {
            public Attribute Type;
            public TextureFormat Format;
            public ushort Width;
            public ushort Height;
            public ushort Depth;
            public ushort MipLevels;
            public fixed uint LodOffset[3];
            public fixed uint OffsetToSurface[13];
        };

        /// <summary>
        /// Specify preprocessing texture data for consumption in DXGI.
        /// </summary>
        public enum DxgiFormatConversion
        {
            /// <summary>
            /// No conversion is required.
            /// </summary>
            NoConversion,
            
            /// <summary>
            /// Conversion from L8 (8bpp) to B8G8R8A8 (32bpp) is required.
            /// Each byte indicates color value for each RGB channel. Alpha channel is fixed to 255.
            /// </summary>
            FromL8ToB8G8R8A8,
            
            /// <summary>
            /// Conversion from B4G4R4A4 (16bpp) to B8G8R8A8 (32bpp) is required.
            /// </summary>
            FromB4G4R4A4ToB8G8R8A8,
            
            /// <summary>
            /// Conversion from B5G5R5A1 (16bpp) to B8G8R8A8 (32bpp) is required.
            /// </summary>
            FromB5G5R5A1ToB8G8R8A8,
        }

        private byte[]? _imageDataDefault;

        public TexHeader Header;

        public int HeaderLength => Unsafe.SizeOf< TexHeader >();

        /// <summary>
        /// Parsed texture buffer, in original texture format.
        /// </summary>
        public TextureBuffer TextureBuffer;

        /// <summary>
        /// The converted A8R8G8B8 image, taking the first Z/face/mipmap.
        /// </summary>
        public byte[] ImageData
        {
            get
            {
                _imageDataDefault ??= TextureBuffer.Filter( mip: 0, z: 0, format: TextureFormat.B8G8R8A8 ).RawData;
                return _imageDataDefault;
            }
        }

        public override void LoadFile()
        {
            Reader.BaseStream.Position = 0;
            Header = Reader.ReadStructure< TexHeader >();

            if( ( Header.Type & Attribute.TextureTypeCube ) != 0 && Header.Depth != 1 )
                throw new NotSupportedException( "Cube map texture with depth value above 1 is currently not supported." );

            TextureBuffer = TextureBuffer.FromStream( Header, Reader );
        }

        /// <summary>
        /// Get DXGI_FORMAT and required preprocessing from TextureFormat.
        /// </summary>
        /// <param name="format">.tex texture format value.</param>
        /// <param name="useGameCompatible">Whether to emulate the game on preprocessing texture data.</param>
        /// <remarks>
        /// Values are taken from v6.15 ffxiv_dx11.exe+0x321f80.
        /// </remarks>
        public static Tuple< int, DxgiFormatConversion > GetDxgiFormatFromTextureFormat( TextureFormat format, bool useGameCompatible = true )
        {
            return format switch
            {
                TextureFormat.Unknown => Tuple.Create( 0x00, DxgiFormatConversion.NoConversion ), // DXGI_FORMAT_UNKNOWN
                TextureFormat.Null => Tuple.Create( 0x00, DxgiFormatConversion.NoConversion ), // DXGI_FORMAT_UNKNOWN
                TextureFormat.R32G32B32A32F => Tuple.Create( 0x02, DxgiFormatConversion.NoConversion ), // DXGI_FORMAT_R32G32B32A32_FLOAT
                TextureFormat.R16G16B16A16F => Tuple.Create( 0x0a, DxgiFormatConversion.NoConversion ), // DXGI_FORMAT_R16G16B16A16_FLOAT
                TextureFormat.R32G32F => Tuple.Create( 0x10, DxgiFormatConversion.NoConversion ), // DXGI_FORMAT_R32G32_FLOAT
                TextureFormat.R16G16F => Tuple.Create( 0x22, DxgiFormatConversion.NoConversion ), // DXGI_FORMAT_R16G16_FLOAT 
                TextureFormat.R32F => Tuple.Create( 0x29, DxgiFormatConversion.NoConversion ), // DXGI_FORMAT_R32_FLOAT
                TextureFormat.D24S8 => Tuple.Create( 0x2c, DxgiFormatConversion.NoConversion ), // DXGI_FORMAT_R24G8_TYPELESS
                TextureFormat.Shadow24 => Tuple.Create( 0x2c, DxgiFormatConversion.NoConversion ), // DXGI_FORMAT_R24G8_TYPELESS
                TextureFormat.D16 => Tuple.Create( 0x35, DxgiFormatConversion.NoConversion ), // DXGI_FORMAT_R16_TYPELESS
                TextureFormat.Shadow16 => Tuple.Create( 0x35, DxgiFormatConversion.NoConversion ), // DXGI_FORMAT_R16_TYPELESS
                TextureFormat.A8 => Tuple.Create( 0x41, DxgiFormatConversion.NoConversion ), // DXGI_FORMAT_A8_UNORM
                TextureFormat.BC1 => Tuple.Create( 0x47, DxgiFormatConversion.NoConversion ), // DXGI_FORMAT_BC1_UNORM
                TextureFormat.BC2 => Tuple.Create( 0x4a, DxgiFormatConversion.NoConversion ), // DXGI_FORMAT_BC2_UNORM
                TextureFormat.BC3 => Tuple.Create( 0x4d, DxgiFormatConversion.NoConversion ), // DXGI_FORMAT_BC3_UNORM
                TextureFormat.BC5 => Tuple.Create( 0x53, DxgiFormatConversion.NoConversion ), // DXGI_FORMAT_BC5_UNORM
                TextureFormat.L8 => Tuple.Create( 0x57, DxgiFormatConversion.FromL8ToB8G8R8A8 ), // each pixel is RGBA(x, x, x, 255)
                TextureFormat.B4G4R4A4 => useGameCompatible
                    ? Tuple.Create( 0x57, DxgiFormatConversion.FromB4G4R4A4ToB8G8R8A8 ) // DXGI_FORMAT_B8G8R8A8_UNORM
                    : Tuple.Create( 0x73, DxgiFormatConversion.NoConversion ) // DXGI_FORMAT_B4G4R4A4_UNORM
                , // DXGI_FORMAT_B4G4R4A4_UNORM(0x73): unsupported in dx10, dx10.1, dx11, and dx11.1 (before windows8)
                TextureFormat.B5G5R5A1 =>useGameCompatible
                    ? Tuple.Create( 0x57, DxgiFormatConversion.FromB5G5R5A1ToB8G8R8A8 ) // DXGI_FORMAT_B8G8R8A8_UNORM
                    : Tuple.Create( 0x56, DxgiFormatConversion.NoConversion ) // DXGI_FORMAT_B5G5R5A1_UNORM
                , // DXGI_FORMAT_B5G5R5A1_UNORM(0x56): unsupported in dx10, dx10.1, dx11, and dx11.1 (before windows8)
                TextureFormat.B8G8R8A8 => Tuple.Create( 0x57, DxgiFormatConversion.NoConversion ), // DXGI_FORMAT_B8G8R8A8_UNORM
                TextureFormat.B8G8R8X8 => Tuple.Create( 0x58, DxgiFormatConversion.NoConversion ), // DXGI_FORMAT_B8G8R8X8_UNORM
                TextureFormat.BC7 => Tuple.Create( 0x62, DxgiFormatConversion.NoConversion ), // DXGI_FORMAT_BC7_UNORM
                _ => throw new NotSupportedException($"TextureFormat {(int)format:X04} is not supported."),
            };
        }
    }
}