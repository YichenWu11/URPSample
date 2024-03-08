using UnityEngine;
using UnityEngine.Rendering.Universal;

public class BuddyAllocatorTest : MonoBehaviour
{
    private BuddyAllocator m_Allocator;

    private BuddyAllocation[] m_Allocations;

    public int level = 0;
    public int length = 10;
    public int branchingOrder = 1;
    public int levelCount = 5;

    public void AllocateTest0()
    {
        m_Allocator = new BuddyAllocator(levelCount, branchingOrder);
        m_Allocations = new BuddyAllocation[length];

        int sum = 0;

        for (int i = 0; i < m_Allocations.Length; ++i)
        {
            if (m_Allocator.TryAllocate(level, out m_Allocations[i]))
            {
                Debug.Log($"level:{m_Allocations[i].level}; index:{m_Allocations[i].index}");
                sum++;
            }
        }

        Debug.Log($"sum:{sum}");

        for (int i = 0; i < m_Allocations.Length; ++i)
        {
            m_Allocator.Free(m_Allocations[i]);
        }
    }

    public void AllocateTest1()
    {
        // Debug.Log($"length: {BuddyAllocator.LevelLength(level, branchingOrder)}; offset: {BuddyAllocator.LevelOffset(level, branchingOrder)}");
        // Debug.Log($"length64: {BuddyAllocator.LevelLength64(level, branchingOrder)}; offset64: {BuddyAllocator.LevelOffset64(level, branchingOrder)}");
    }
}
