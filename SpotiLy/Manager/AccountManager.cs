using SpotiLy.SpotifyApi;
using System;
using System.Collections.Generic;

namespace SpotiLy.Manager
{
    public sealed class AccountManager
    {
        private static readonly Lazy<AccountManager> lazy =
            new Lazy<AccountManager>(() => new AccountManager(), true);

        public static AccountManager Instance { get { return lazy.Value; } }

        private readonly Stack<Account> accountsOk;
        private readonly Stack<Account> accountsKo;
        private AccountManager()
        {
            accountsOk = new Stack<Account>();
            accountsKo = new Stack<Account>();
        }

        public Account GetAccount()
        {
            if (accountsOk.Count > 0)
            {
                return accountsOk.Pop();
            }
            else
            {
                throw new Exception("No more accounts");
            }
        }

        public void Enable(Account account)
        {
            accountsOk.Push(account);
        }

        public void Disable(Account account)
        {
            accountsKo.Push(account);
        }
    }
}