using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightImgur
{
    public class ReceivedData<T>
    {
        public List<T> Gallery;


        public ReceivedData()
        {
            Gallery = new List<T>();
        }
    }
}
