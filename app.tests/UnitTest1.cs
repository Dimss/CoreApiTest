using NUnit.Framework;
using app.Models;
using app.Dal;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore;
using System;

using System.IO;

namespace Tests
{
    [TestFixture]
    public class Tests
    {

        private Person getTestPerson(){
            Person p = new Person();
            p.ID = "123";
            p.FirstName = "TestFirstName";
            p.LastName = "TestLastName";
            p.Age = 22;
            return p;
        }

        private IConfiguration getConf(){
          return new ConfigurationBuilder()
          .AddJsonFile("/Users/dima/code/govil/CoreApiTest/app.tests/appsettings.json")
          .AddEnvironmentVariables()
          .Build();
        }

        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void TestCreatePerson()
        {

            var config = getConf();
            DB_Config dbConfig = new DB_Config();
            dbConfig.DbConnectionString = config["ControllerSettings:DbConfig:DbConnectionString"];
            dbConfig.DbName = config["ControllerSettings:DbConfig:DbName"];
            dbManager _dbManager = new dbManager(dbConfig);
            _dbManager.InsertNewPerson(getTestPerson());
            Assert.Pass();
        }

        [Test]
        public void TestGetPersonById()
        {
            var config = getConf();
            DB_Config dbConfig = new DB_Config();
            dbConfig.DbConnectionString = config["ControllerSettings:DbConfig:DbConnectionString"];
            dbConfig.DbName = config["ControllerSettings:DbConfig:DbName"];
            dbManager _dbManager = new dbManager(dbConfig);
            Person testPerson = getTestPerson();
            Person person  = _dbManager.GetPerson(testPerson.ID);
            Assert.AreEqual(person.ID,testPerson.ID);
        }

        [Test]
        public void TestGetPersonsList()
        {
            var config = getConf();
            DB_Config dbConfig = new DB_Config();
            dbConfig.DbConnectionString = config["ControllerSettings:DbConfig:DbConnectionString"];
            dbConfig.DbName = config["ControllerSettings:DbConfig:DbName"];
            dbManager _dbManager = new dbManager(dbConfig);
            Assert.That(_dbManager.GetPersonsList(), Is.Not.Empty);
        }

        [Test]
        public void TestUpdatePerson()
        {
            var config = getConf();
            DB_Config dbConfig = new DB_Config();
            dbConfig.DbConnectionString = config["ControllerSettings:DbConfig:DbConnectionString"];
            dbConfig.DbName = config["ControllerSettings:DbConfig:DbName"];
            dbManager _dbManager = new dbManager(dbConfig);
            Person testPerson = getTestPerson();
            testPerson.FirstName = "UpdatedFirstName";
            _dbManager.UpdatePerson(testPerson);
            Person personFromDb =  _dbManager.GetPerson(testPerson.ID);
            Assert.AreEqual(personFromDb.FirstName, testPerson.FirstName);
        }


        [OneTimeTearDown]
        public void Cleanup()
        {
            var config = getConf();
            DB_Config dbConfig = new DB_Config();
            dbConfig.DbConnectionString = config["ControllerSettings:DbConfig:DbConnectionString"];
            dbConfig.DbName = config["ControllerSettings:DbConfig:DbName"];
            dbManager _dbManager = new dbManager(dbConfig);
            Person testPerson = getTestPerson();
            _dbManager.DeletePerson(testPerson.ID);

        }
    }
}
