using System.Collections.ObjectModel;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Maze.Engine
{
    public class UpdateableManager : Collection<IUpdateable>
    {
        private readonly List<IUpdateable> _deleteList;
        private readonly List<(int index, IUpdateable item)> _addList;

        public UpdateableManager() =>
            (_deleteList, _addList) = (new List<IUpdateable>(), new List<(int, IUpdateable)>());

        public void Update(GameTime time)
        {
            foreach ((var index, var item) in _addList)
            {
                if (index >= Count)
                    Items.Add(item);
                else if (!Items.Contains(item))
                    Items.Insert(index, item);
            }
            _addList.Clear();

            for (int i =0; i < Items.Count; i++)
            {
                if (Items[i].Update(time))
                {
                    Items[i].End();
                    _deleteList.Add(Items[i]);
                }
            }

            foreach (var item in _deleteList)
                Items.Remove(item);
            _deleteList.Clear();
        }

        protected override void ClearItems()
        {
            foreach (var item in Items)
                item.End();
            base.ClearItems();
        }

        protected override void InsertItem(int index, IUpdateable item)
        {
            _addList.Add((index, item));
            item.Begin();
        }

        protected override void RemoveItem(int index)
        {
            _deleteList.Add(Items[index]);
            Items[index].End();
        }

        protected override void SetItem(int index, IUpdateable item)
        {
            Items[index].End();

            item.Begin();
            Items[index] = item;
        }
    }
}
