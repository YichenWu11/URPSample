using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace UnityEngine.Rendering.Universal
{
    [BurstCompile(FloatMode = FloatMode.Fast, DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
    struct TileRangeExpansionJob : IJobFor
    {
        [ReadOnly]
        public NativeArray<InclusiveRange> tileRanges;

        [NativeDisableParallelForRestriction]
        public NativeArray<uint> tileMasks;

        public int rangesPerItem;
        public int itemsPerTile;
        public int wordsPerTile;
        public int2 tileResolution;

        public void Execute(int jobIndex)
        {
            // Per Row
            var rowIndex = jobIndex % tileResolution.y;
            var viewIndex = jobIndex / tileResolution.y;
            var compactCount = 0;
            var itemIndices = new NativeArray<short>(itemsPerTile, Allocator.Temp);
            var itemRanges = new NativeArray<InclusiveRange>(itemsPerTile, Allocator.Temp);

            // Compact the light ranges for the current row.
            for (var itemIndex = 0; itemIndex < itemsPerTile; itemIndex++)
            {
                var range = tileRanges[viewIndex * rangesPerItem * itemsPerTile + itemIndex * rangesPerItem + 1 + rowIndex];
                // skip the empty range
                if (!range.isEmpty)
                {
                    itemIndices[compactCount] = (short)itemIndex;
                    itemRanges[compactCount] = range;
                    compactCount++;
                }
            }

            // rowIndex * wordsPerTile * tileResolution.x
            // 每行有 tileResolution 个 tile (x 方向上)
            // 因此 Offset 是 rowIndex * (wordsPerTile * tileResolution.x)
            var rowBaseMaskIndex = viewIndex * wordsPerTile * tileResolution.x * tileResolution.y + rowIndex * wordsPerTile * tileResolution.x;
            // 对当前行 x 方向上每个 Tile
            // 逐 Item 检查有没有覆盖
            for (var tileIndex = 0; tileIndex < tileResolution.x; tileIndex++)
            {
                // tileIndex (x 方向上)
                var tileBaseIndex = rowBaseMaskIndex + tileIndex * wordsPerTile;
                for (var i = 0; i < compactCount; i++)
                {
                    var itemIndex = (int)itemIndices[i];
                    var wordIndex = itemIndex / 32;        // 该 item 在哪一个 uint 上
                    var itemMask = 1u << (itemIndex % 32); // 该 item 在哪一位上
                    var range = itemRanges[i];
                    if (range.Contains((short)tileIndex))
                    {
                        tileMasks[tileBaseIndex + wordIndex] |= itemMask;
                    }
                }
            }

            itemIndices.Dispose();
            itemRanges.Dispose();
        }
    }
}
