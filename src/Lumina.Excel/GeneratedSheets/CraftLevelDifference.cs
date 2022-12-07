// ReSharper disable All

using Lumina.Text;
using Lumina.Data;
using Lumina.Data.Structs.Excel;

namespace Lumina.Excel.GeneratedSheets
{
    [Sheet( "CraftLevelDifference", columnHash: 0xb5d07117 )]
    public partial class CraftLevelDifference : ExcelRow
    {
        
        public short Difference { get; set; }
        
        public override void PopulateData( RowParser parser, GameData gameData, Language language )
        {
            base.PopulateData( parser, gameData, language );

            Difference = parser.ReadColumn< short >( 0 );
        }
    }
}