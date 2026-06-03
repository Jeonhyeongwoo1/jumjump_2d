using JumJump.Controller;

namespace JumJump.Registry
{
    /// <summary>
    /// 런타임에 풀에서 가져온 플레이어 인스턴스를 보관한다. 소유권은 갖지 않는다.
    /// </summary>
    public sealed class PlayerRegistry
    {
        public PlayerJumpController Player { get; private set; }

        public void Set(PlayerJumpController player)
        {
            Player = player;
        }

        public void Clear()
        {
            Player = null;
        }
    }
}
