using System.Runtime.CompilerServices;
using Terraria;

namespace Chireiden.TShock.Omni;

public partial class Plugin
{
    public class CheckedTypedCollection : ModFramework.ICollection<ITile>
    {
        internal Tile[,] _items = new Tile[0, 0];
        public int Width { get; }
        public int Height { get; }
        public void Resize(int width, int height)
        {
            this._items = new Tile[width, height];
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    this._items[x, y] = new Tile();
                }
            }
        }

        public ITile this[int x, int y]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            get => this._items[x, y];
            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            set => this._items[x, y] = (Tile) value;
        }
    }

    public class CheckedGenericCollection : ModFramework.ICollection<ITile>
    {
        internal ITile[,] _items = new ITile[0, 0];
        public int Width { get; }
        public int Height { get; }
        public void Resize(int width, int height)
        {
            this._items = new ITile[width, height];
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    this._items[x, y] = new Tile();
                }
            }
        }

        public ITile this[int x, int y]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            get => this._items[x, y];
            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            set => this._items[x, y] = value;
        }
    }
}