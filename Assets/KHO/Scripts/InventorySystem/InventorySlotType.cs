namespace KHO.Scripts.InventorySystem
{
    public enum InventorySlotType
    {
        Equipment = 0,  // 장착: 효과가 적용된다
        Storage = 1,    // 보관: 효과가 적용되지 않는다(20칸, 창에서는 스크롤로 본다)
    }
}
