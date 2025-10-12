using System;

namespace Kuantech.Core.Database
{
    [Serializable]

    public abstract class Balancer
    {
        public abstract void Balance(KtDatabase db, string tableName);
    }
}