using System;

namespace Bookworm_Bot_Class
{
	[Flags]
	public enum WordCategory
	{
		None = 0,
		Colors = 1,
		Metals = 2,
		Mammals = 4,
		Felines = 8,
		Bone = 16,
		Fire = 32,
		FruitsAndVegetables = 64,
		Adjectives = 128,
		Verbs = 256,
		Words = 512
	}
}