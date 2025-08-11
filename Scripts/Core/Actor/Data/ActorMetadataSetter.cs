using Kuantech.Midcore;

namespace Kuantech.Core
{
    public class ActorMetadataSetter : ActorBlueprintComponent
    {
        public int FactionId;
        public string DropCurrencyId = "IngameCoin";
        public int DropCurrencyAmount = 1;
        
        public override void OnActorCreated(ActorBlueprint blueprint, Actor actor)
        {
            actor.FactionId = FactionId;
            CoinDropModule coinDropModule = actor.GetModule<CoinDropModule>();
            //Set drop currency
            if(coinDropModule != null)
            {
                coinDropModule.CurrencyData.CurrencyId = DropCurrencyId;
                coinDropModule.CurrencyData.CurrencyAmount = DropCurrencyAmount;
            }
        }
    }
}