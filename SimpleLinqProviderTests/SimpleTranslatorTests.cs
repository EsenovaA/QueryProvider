using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using SimpleLinqProvider.Entities;
using Xunit;

namespace SimpleLinqProvider.Tests
{
    public class SimpleTranslatorTests
    {
        [Fact]
        public void TranslateTest()
        {
            var productSet = new List<ProductEntity>();
            
            var translator = new SimpleTranslator();
            Expression<Func<IQueryable<ProductEntity>, IQueryable<ProductEntity>>> expression
                = query => (IQueryable<ProductEntity>)query.Where(p => p.UnitPrice > 100 && p.ProductType == "Customised Product").ToList();

            var actualResult = translator.Translate(expression);

            var expectedResult =
                "SELECT * FROM [dbo].[products] WHERE UnitPrice > 100 AND ProductType = 'Customised Product'";

            Assert.Equal(expectedResult, actualResult);
        }
    }
}