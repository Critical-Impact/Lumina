// ReSharper disable All

using Lumina.Text;
using Lumina.Data;
using Lumina.Data.Structs.Excel;

namespace Lumina.Excel.GeneratedSheets
{
    [Sheet( "FCCrestSymbol", columnHash: 0x92a0df1e )]
    public partial class FCCrestSymbol : ExcelRow
    {
        
        public byte ColorNum { get; set; }
        public byte FCRight { get; set; }
        public ushort Unknown2 { get; set; }
        
        public override void PopulateData( RowParser parser, GameData gameData, Language language )
        {
            base.PopulateData( parser, gameData, language );

            ColorNum = parser.ReadColumn< byte >( 0 );
            FCRight = parser.ReadColumn< byte >( 1 );
            Unknown2 = parser.ReadColumn< ushort >( 2 );
        }
    }
}