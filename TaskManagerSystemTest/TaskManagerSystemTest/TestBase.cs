using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TaskManagerSystem.Data;

namespace TaskManagerSystem.Tests
{
    public abstract class TestBase
    {
        protected AppDbContext GetDatabase()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        protected void SetupControllerContext(Controller controller)
        {
            var httpContext = new DefaultHttpContext();
            var modelMetadataProvider = new EmptyModelMetadataProvider();
            var viewData = new ViewDataDictionary(modelMetadataProvider, new ModelStateDictionary());
            var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            controller.ViewData = viewData;
            controller.TempData = tempData;
            httpContext.Session = new MockSession();
        }

        public class MockSession : ISession
        {
            private readonly Dictionary<string, byte[]> _sessionStorage = new();
            public bool IsAvailable => true;
            public string Id => Guid.NewGuid().ToString();
            public IEnumerable<string> Keys => _sessionStorage.Keys;
            public void Clear() => _sessionStorage.Clear();
            public Task CommitAsync(CancellationToken ct = default) => Task.CompletedTask;
            public Task LoadAsync(CancellationToken ct = default) => Task.CompletedTask;
            public void Remove(string key) => _sessionStorage.Remove(key);
            public void Set(string key, byte[] value) => _sessionStorage[key] = value;
            public bool TryGetValue(string key, out byte[] value) => _sessionStorage.TryGetValue(key, out value);
        }
    }
}