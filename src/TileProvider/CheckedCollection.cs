using System.Runtime.CompilerServices;
using Terraria;
using TerrariaApi.Server;

namespace Chireiden.TShock.Omni;

public partial class Plugin : TerrariaPlugin
{
    public class CheckedTypedCollection : ModFramework.ICollection<Terraria.ITile>
    {
        internal Terraria.Tile[,] _items = new Terraria.Tile[0, 0];
        public int Width { get; private set; }
        public int Height { get; private set; }
        public void Resize(int width, int height)
        {
            this._items = new Terraria.Tile[width, height];
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    this._items[x, y] = new Tile();
                }
            }
        }

        public Terraria.ITile this[int x, int y]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            get => this._items[x, y];
            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            set => this._items[x, y] = (Tile) value;
        }
    }

    public class CheckedGenericCollection : ModFramework.ICollection<Terraria.ITile>
    {
        internal Terraria.ITile[,] _items = new Terraria.ITile[0, 0];
        public int Width { get; private set; }
        public int Height { get; private set; }
        public void Resize(int width, int height)
        {
            this._items = new Terraria.ITile[width, height];
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    this._items[x, y] = new Tile();
                }
            }
        }

        public Terraria.ITile this[int x, int y]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            get => this._items[x, y];
            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            set => this._items[x, y] = value;
        }
    }
}
