﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RomUtilities;
using static System.Math;
using System.Collections;

namespace FF1Lib
{
	// ReSharper disable once InconsistentNaming
	public partial class FF1Rom : NesRom
	{
		public const string Version = "1.4.5";

		public const int CopyrightOffset1 = 0x384A8;
		public const int CopyrightOffset2 = 0x384BA;

		public const int RngOffset = 0x3F100;
		public const int RngSize = 256;

		public FF1Rom(string filename) : base(filename)
		{}

		public void Randomize(Blob seed, Flags flags)
		{
			var rng = new MT19337(BitConverter.ToUInt32(seed, 0));

			EasterEggs();

			// This has to be done before we shuffle spell levels.
			if (flags.SpellBugs)
			{
				FixSpellBugs();
			}

			if (flags.Treasures)
			{
				ShuffleTreasures(rng, flags.EarlyCanoe, flags.EarlyOrdeals, flags.IncentivizeIceCave, flags.IncentivizeOrdeals);
			}

			if (flags.Shops)
			{
				ShuffleShops(rng, flags.EnemyStatusAttacks);
			}

			if (flags.MagicShops)
			{
				ShuffleMagicShops(rng);
			}

			if (flags.MagicLevels)
			{
				ShuffleMagicLevels(rng, flags.MagicPermissions);
			}

			if (flags.Rng)
			{
				ShuffleRng(rng);
			}

			if (flags.EnemyScripts)
			{
				ShuffleEnemyScripts(rng);
			}

			if (flags.EnemySkillsSpells)
			{
				ShuffleEnemySkillsSpells(rng);
			}

			if (flags.EnemyStatusAttacks)
			{
				ShuffleEnemyStatusAttacks(rng);
			}

			if (flags.Ordeals)
			{
				ShuffleOrdeals(rng);
			}

			if (flags.EarlyOrdeals)
			{
				EnableEarlyOrdeals();
			}

			if (flags.EarlyRod)
			{
				EnableEarlyRod();
			}

			if (flags.EarlyCanoe)
			{
				EnableEarlyCanoe();
			}

			if (flags.NoPartyShuffle)
			{
				DisablePartyShuffle();
			}

			if (flags.SpeedHacks)
			{
				EnableSpeedHacks();
			}

			if (flags.IdentifyTreasures)
			{
				EnableIdentifyTreasures();
			}

			if (flags.Dash)
			{
				EnableDash();
			}

			if (flags.BuyTen)
			{
				EnableBuyTen();
			}

			if (flags.HouseMPRestoration)
			{
				FixHouse();
			}

			if (flags.WeaponStats)
			{
				FixWeaponStats();
			}

			if (flags.ChanceToRun)
			{
				FixChanceToRun();
			}

			if (flags.EnemyStatusAttackBug)
			{
				FixEnemyStatusAttackBug();
			}

			if (flags.PriceScaleFactor > 1)
			{
				ScalePrices(flags.PriceScaleFactor, rng);
			}

			if (flags.EnemyScaleFactor > 1)
			{
				ScaleEnemyStats(flags.EnemyScaleFactor, rng);
			}

			if (flags.ExpMultiplier > 1 || flags.ExpBonus > 0)
			{
				ExpGoldBoost(flags.ExpBonus * 10, flags.ExpMultiplier);
			}

			WriteSeedAndFlags(Version, seed.ToHex(), EncodeFlagsText(flags));
		}

		public override bool Validate()
		{
			return Get(0, 16) == Blob.FromHex("06400e890e890e401e400e400e400b42");
		}

		public void WriteSeedAndFlags(string version, string seed, string flags)
		{
			var seedBytes = FF1Text.TextToBytes($"{version}  {seed}");
			var flagBytes = FF1Text.TextToBytes($"{flags}");
			var padding = new byte[15 - flagBytes.Length];
			for (int i = 0; i < padding.Length; i++)
			{
				padding[i] = 0xFF;
			}

			Put(CopyrightOffset1, seedBytes);
			Put(CopyrightOffset2, padding + flagBytes);
		}

		public void ShuffleRng(MT19337 rng)
		{
			var rngTable = Get(RngOffset, RngSize).Chunk(1).ToList();
			rngTable.Shuffle(rng);

			Put(RngOffset, rngTable.SelectMany(blob => blob.ToBytes()).ToArray());
		}

		public void ExpGoldBoost(double bonus, double multiplier)
		{
			var enemyBlob = Get(EnemyOffset, EnemySize * EnemyCount);
			var enemies = enemyBlob.Chunk(EnemySize);

			foreach (var enemy in enemies)
			{
				var exp = BitConverter.ToUInt16(enemy, 0);
				var gold = BitConverter.ToUInt16(enemy, 2);

				exp = (ushort)Min(bonus + exp * multiplier, 0x7FFF);
				gold = (ushort)Min(bonus + gold * multiplier, 0x7FFF);

				var expBytes = BitConverter.GetBytes(exp);
				var goldBytes = BitConverter.GetBytes(gold);
				Array.Copy(expBytes, 0, enemy, 0, 2);
				Array.Copy(goldBytes, 0, enemy, 2, 2);
			}

			enemyBlob = Blob.Concat(enemies);

			Put(EnemyOffset, enemyBlob);
		}

		public static string EncodeFlagsText(Flags flags)
		{
			var bits = new BitArray(25);

			bits[0] = flags.Treasures;
			bits[1] = flags.IncentivizeIceCave;
			bits[2] = flags.IncentivizeOrdeals;
			bits[3] = flags.Shops;
			bits[4] = flags.MagicShops;
			bits[5] = flags.MagicLevels;
			bits[6] = flags.MagicPermissions;
			bits[7] = flags.Rng;
			bits[8] = flags.EnemyScripts;
			bits[9] = flags.EnemySkillsSpells;
			bits[10] = flags.EnemyStatusAttacks;
			bits[11] = flags.Ordeals;

			bits[12] = flags.EarlyRod;
			bits[13] = flags.EarlyCanoe;
			bits[14] = flags.EarlyOrdeals;
			bits[15] = flags.NoPartyShuffle;
			bits[16] = flags.SpeedHacks;
			bits[17] = flags.IdentifyTreasures;
			bits[18] = flags.Dash;
			bits[19] = flags.BuyTen;

			bits[20] = flags.HouseMPRestoration;
			bits[21] = flags.WeaponStats;
			bits[22] = flags.ChanceToRun;
			bits[23] = flags.SpellBugs;
			bits[24] = flags.EnemyStatusAttackBug;

			var bytes = new byte[4];
			// Freaking .NET Core doesn't have BitArray.CopyTo
			for (int i = 0; i < bits.Length; i++)
			{
				bytes[i / 8] |= (byte)((bits[i] ? 1 : 0) << (i % 8));
			}

			var text = Convert.ToBase64String(bytes);
			text = text.TrimEnd('=');
			text = text.Replace('+', '!');
			text = text.Replace('/', '%');

			text += SliderToBase64((int)(10 * flags.PriceScaleFactor));
			text += SliderToBase64((int)(10 * flags.EnemyScaleFactor));
			text += SliderToBase64((int)(10 * flags.ExpMultiplier));
			text += SliderToBase64((int)flags.ExpBonus);

			return text;
		}

		public static Flags DecodeFlagsText(string text)
		{
			var bitString = text.Substring(0, 6);
			bitString = bitString.Replace('!', '+');
			bitString = bitString.Replace('%', '/');
			bitString += "==";

			var bytes = Convert.FromBase64String(bitString);
			var bits = new BitArray(bytes);

			return new Flags
			{
				Treasures = bits[0],
				IncentivizeIceCave = bits[1],
				IncentivizeOrdeals = bits[2],
				Shops = bits[3],
				MagicShops = bits[4],
				MagicLevels = bits[5],
				MagicPermissions = bits[6],
				Rng = bits[7],
				EnemyScripts = bits[8],
				EnemySkillsSpells = bits[9],
				EnemyStatusAttacks = bits[10],
				Ordeals = bits[11],

				EarlyRod = bits[12],
				EarlyCanoe = bits[13],
				EarlyOrdeals = bits[14],
				NoPartyShuffle = bits[15],
				SpeedHacks = bits[16],
				IdentifyTreasures = bits[17],
				Dash = bits[18],
				BuyTen = bits[19],

				HouseMPRestoration = bits[20],
				WeaponStats = bits[21],
				ChanceToRun = bits[22],
				SpellBugs = bits[23],
				EnemyStatusAttackBug = bits[24],

				PriceScaleFactor = Base64ToSlider(text[6]) / 10.0,
				EnemyScaleFactor = Base64ToSlider(text[7]) / 10.0,
				ExpMultiplier = Base64ToSlider(text[8]) / 10.0,
				ExpBonus = Base64ToSlider(text[9])
			};
		}

		private static char SliderToBase64(int value)
		{
			if (value < 0 || value > 63)
			{
				throw new ArgumentOutOfRangeException(nameof(value), value, "Value must be between 0 and 63.");
			}
			else if (value < 10)
			{
				return (char)('0' + value);
			}
			else if (value < 36)
			{
				return (char)('A' + value - 10);
			}
			else if (value < 62)
			{
				return (char)('a' + value - 36);
			}
			else if (value == 62)
			{
				return '!';
			}
			else
			{
				return '%';
			}
		}

		private static int Base64ToSlider(char value)
		{
			if (value >= '0' && value <= '9')
			{
				return value - '0';
			}
			else if (value >= 'A' && value <= 'Z')
			{
				return value - 'A' + 10;
			}
			else if (value >= 'a' && value <= 'z')
			{
				return value - 'a' + 36;
			}
			else if (value == '!')
			{
				return 62;
			}
			else
			{
				return 63;
			}
		}
	}
}
