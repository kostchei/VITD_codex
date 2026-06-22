namespace VastDark.Domain;

public enum CrawlCreature { Cyclops, Medusa, Harpy, Griffon, Siren, Centaur, Hydra, Shade, Ogre, Wyrm }
public sealed record CrawlCreatureRule(CrawlCreature Creature, int HitDice, int HitPoints, string Move, string Defense, string Attack, string Special);

public static class CrawlCreatureRules
{
    private static readonly IReadOnlyDictionary<CrawlCreature, CrawlCreatureRule> Rules = new Dictionary<CrawlCreature, CrawlCreatureRule>
    {
        [CrawlCreature.Cyclops] = new(CrawlCreature.Cyclops,2,10,"Standard","Hide","Fist/Claw 1d4/1d4","Call in Dark: 1-in-6 another Cyclops each aware round."), [CrawlCreature.Medusa] = new(CrawlCreature.Medusa,5,25,"Standard","Hide","Bite 1d8","Petrifying Scream: Charm save; fail stunned 1 minute, success grants subsequent advantage."), [CrawlCreature.Harpy] = new(CrawlCreature.Harpy,2,8,"Fly Standard","Leather","Whip 1d6 and Meld","Breath save or adheres; then 1d6/turn and heals 1d3."), [CrawlCreature.Griffon] = new(CrawlCreature.Griffon,8,50,"Fly Standard","Hide","Maw 1d8 and Devour","Hold save or trapped, 2d6/turn; at 15 damage save or swallowed."), [CrawlCreature.Siren] = new(CrawlCreature.Siren,8,40,"None","Plate","Crushing Maw","Charm/Poison save against mist; Maw 3d6 + exhaustion + random item loss."),
        [CrawlCreature.Centaur] = new(CrawlCreature.Centaur,6,24,"Climb Double","Leather","1d6 Limbs 1d4","Move full speed when harmed; automatically passes Breath saves."), [CrawlCreature.Hydra] = new(CrawlCreature.Hydra,10,55,"Standard","Hide","1d4 Bites 1d8 and Venom","When harmed 1-in-6 gains 1d6 attacks; poison success exhaustion, fail 1d3 exhaustion + 1d3 hours paralysis."), [CrawlCreature.Shade] = new(CrawlCreature.Shade,4,36,"Fly Standard","Chain-shirt","Swarm 2d6","Automatically hits; fire/explosion vulnerable; 1-in-6 blind 1d3 days after hit."), [CrawlCreature.Ogre] = new(CrawlCreature.Ogre,10,100,"Half","Hide","Slam 2d6 or Engulf","Hold save or engulfed for 3d6/turn; at 10 HP splits into 1d6 1HD/5HP spawns."), [CrawlCreature.Wyrm] = new(CrawlCreature.Wyrm,15,150,"Fly Double Standard","Scale","Claws 1d8/1d8 and Maw 2d10 or Howl","Mimics heard mortals; Howl Breath save, success half, failure 3d6 sonic + deaf 1d6 hours."),
    };
    public static CrawlCreatureRule Get(CrawlCreature creature) => Rules[creature];
    public static bool CyclopsCallsAlly(IRandomSource random) => random.Next(1,7) == 1;
    public static (bool Stunned, bool FutureSaveAdvantage) MedusaScream(bool charmSaveSucceeded) => charmSaveSucceeded ? (false,true) : (true,false);
    public static (bool Adheres, int DamagePerTurn, int HealPerTurn) HarpyMeld(bool covered, bool breathSaveSucceeded, bool spentTurnKeepingAway, IRandomSource random) => covered || breathSaveSucceeded || spentTurnKeepingAway ? (false,0,0) : (true,random.Next(1,7),random.Next(1,4));
    public static bool GriffonSwallows(int accumulatedDevourDamage, bool holdSaveSucceeded) => accumulatedDevourDamage >= 15 && !holdSaveSucceeded;
    public static int HydraFrenzyAttacks(IRandomSource random) => random.Next(1,7) == 1 ? random.Next(1,7) : 0;
    public static (int Exhaustion, int ParalysisHours) HydraVenom(bool poisonSaveSucceeded, IRandomSource random) => poisonSaveSucceeded ? (1,0) : (random.Next(1,4),random.Next(1,4));
    public static (bool Blind, int Days) ShadeEyeBite(IRandomSource random) => random.Next(1,7) == 1 ? (true,random.Next(1,4)) : (false,0);
    public static int OgreSpawnCount(int hitPoints, IRandomSource random) => hitPoints == 10 ? random.Next(1,7) : 0;
    public static (int Damage, bool Deafened, int DeafenedHours) WyrmHowl(bool breathSaveSucceeded, IRandomSource random)
    {
        var damage = Enumerable.Range(0,3).Sum(_ => random.Next(1,7));
        return breathSaveSucceeded ? (damage / 2,false,0) : (damage,true,random.Next(1,7));
    }
}
