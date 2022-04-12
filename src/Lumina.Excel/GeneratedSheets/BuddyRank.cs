// ReSharper disable All

using Lumina.Text;
using Lumina.Data;
using Lumina.Data.Structs.Excel;

namespace Lumina.Excel.GeneratedSheets
{
    [Sheet( "BuddyRank", columnHash: 0xdbf43666 )]
    public class BuddyRank : ExcelRow
    {
        
        public uint ExpRequired { get; set; }
        
        public override void PopulateData( RowParser parser, GameData gameData, Language language )
        {
            base.PopulateData( parser, gameData, language );

            ExpRequired = parser.ReadColumn< uint >( 0 );
        }
    }
}