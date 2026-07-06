using JumJump.Controller;
using VContainer;

namespace JumJump.Registry
{
    /// <summary>
    /// 런타임에 풀에서 가져온 플레이어 인스턴스를 보관한다. 소유권은 갖지 않는다.
    /// </summary>
    public sealed class PlayerRegistry
    {
        public Player Player { get; private set; }

        [Inject]
        public PlayerRegistry()
        {
        }

        public void Set(Player player)
        {
            Player = player;
        }

        public void Clear()
        {
            Player = null;
        }
    }
}
