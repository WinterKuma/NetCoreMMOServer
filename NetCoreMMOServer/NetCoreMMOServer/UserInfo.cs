using NetCoreMMOServer.Network;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace NetCoreMMOServer
{
    internal class UserInfo
    {
        private static int s_MaxID = 0;
        private readonly int _id;
        private Vector3 _position;

        [AllowNull]
        private User _user;

        public UserInfo()
        {
            _user = null;
            _id = ++s_MaxID;
        }

        public int Id => _id;
        public Vector3 Position
        {
            get => _position;
            set => _position = value;
        }

        public User User
        {
            get => _user;
            set => _user = value;
        }
    }
}
