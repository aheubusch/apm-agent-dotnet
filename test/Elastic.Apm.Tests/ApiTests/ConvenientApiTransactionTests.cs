﻿// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.Helpers;
using Elastic.Apm.Model;
using Elastic.Apm.Tests.Extensions;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.Tests.ApiTests
{
	/// <summary>
	/// Tests the API for manual instrumentation.
	/// Only tests scenarios when using the convenient API and only test transactions.
	/// Spans are covered by <see cref="ConvenientApiSpanTests" />.
	/// Scenarios with manually calling <see cref="Tracer.StartTransaction" />,
	/// <see cref="Transaction.StartSpan" />, <see cref="Transaction.End" />
	/// are covered by <see cref="ApiTests" />
	/// Very similar to <see cref="ConvenientApiSpanTests" />. The test cases are the same,
	/// but this one tests the CaptureTransaction method - including every single overload.
	/// </summary>
	public class ConvenientApiTransactionTests
	{
		private const string ExceptionMessage = "Foo";

		private const string TransactionName = "ConvenientApiTest";
		private const string TransactionType = "Test";

		public ConvenientApiTransactionTests(ITestOutputHelper testOutputHelper) =>
			testOutputHelper.WriteLine($" Stopwatch.Frequency: {Stopwatch.Frequency}");

		/// <summary>
		/// Tests the <see cref="Tracer.CaptureTransaction(string,string,Action, DistributedTracingData)" /> method.
		/// It wraps a fake transaction (Thread.Sleep) into the CaptureTransaction method
		/// and it makes sure that the transaction is captured by the agent.
		/// </summary>
		[Fact]
		public void SimpleAction() => AssertWith1Transaction(agent =>
		{
			agent.Tracer.CaptureTransaction(TransactionName, TransactionType,
				() => { WaitHelpers.SleepMinimum(); });
		});

		/// <summary>
		/// Tests the <see cref="Tracer.CaptureTransaction(string,string,Action, DistributedTracingData)" /> method with an
		/// exception.
		/// It wraps a fake transaction (Thread.Sleep) that throws an exception into the CaptureTransaction method
		/// and it makes sure that the transaction and the exception are captured by the agent.
		/// </summary>
		[Fact]
		public void SimpleActionWithException() => AssertWith1TransactionAnd1Error(agent =>
		{
			Action act = () =>
			{
				agent.Tracer.CaptureTransaction(TransactionName, TransactionType, new Action(() =>
				{
					WaitHelpers.SleepMinimum();
					throw new InvalidOperationException(ExceptionMessage);
				}));
			};
			act.Should().Throw<InvalidOperationException>();
		});

		/// <summary>
		/// Tests the <see cref="Tracer.CaptureTransaction(string,string,Action, DistributedTracingData)" />
		/// method.
		/// It wraps a fake transaction (Thread.Sleep) into the CaptureTransaction method with an <see cref="Action{T}" />
		/// parameter
		/// and it makes sure that the transaction is captured by the agent and the <see cref="Action{ITransaction}" /> parameter
		/// is not null
		/// </summary>
		[Fact]
		public void SimpleActionWithParameter() => AssertWith1Transaction(agent =>
		{
			agent.Tracer.CaptureTransaction(TransactionName, TransactionType,
				t =>
				{
					t.Should().NotBeNull();
					WaitHelpers.SleepMinimum();
				});
		});

		/// <summary>
		/// Tests the <see cref="Tracer.CaptureTransaction(string,string,Action, DistributedTracingData)" />
		/// method with an exception.
		/// It wraps a fake transaction (Thread.Sleep) that throws an exception into the CaptureTransaction method with an
		/// <see cref="Action{ITransaction}" /> parameter
		/// and it makes sure that the transaction and the error are captured by the agent and the
		/// <see cref="Action{ITransaction}" /> parameter is not null
		/// </summary>
		[Fact]
		public void SimpleActionWithExceptionAndParameter() => AssertWith1TransactionAnd1Error(agent =>
		{
			Action act = () =>
			{
				agent.Tracer.CaptureTransaction(TransactionName, TransactionType, new Action<ITransaction>(t =>
				{
					t.Should().NotBeNull();
					WaitHelpers.SleepMinimum();
					throw new InvalidOperationException(ExceptionMessage);
				}));
			};
			act.Should().Throw<InvalidOperationException>();
		});

		/// <summary>
		/// Tests the <see cref="Tracer.CaptureTransaction{T}(string,string,Func{TResult}, DistributedTracingData)" /> method.
		/// It wraps a fake transaction (Thread.Sleep) with a return value into the CaptureTransaction method
		/// and it makes sure that the transaction is captured by the agent and the return value is correct.
		/// </summary>
		[Fact]
		public void SimpleActionWithReturnType() => AssertWith1Transaction(agent =>
		{
			var res = agent.Tracer.CaptureTransaction(TransactionName, TransactionType, () =>
			{
				WaitHelpers.SleepMinimum();
				return 42;
			});

			res.Should().Be(42);
		});

		/// <summary>
		/// Tests the
		/// <see cref="Tracer.CaptureTransaction{T}(string,string,Func{TResult}, DistributedTracingData)" /> method.
		/// It wraps a fake transaction (Thread.Sleep) with a return value into the CaptureTransaction method with an
		/// <see cref="Action{ITransaction}" /> parameter
		/// and it makes sure that the transaction is captured by the agent and the return value is correct and the
		/// <see cref="Action{ITransaction}" /> is not null.
		/// </summary>
		[Fact]
		public void SimpleActionWithReturnTypeAndParameter() => AssertWith1Transaction(agent =>
		{
			var res = agent.Tracer.CaptureTransaction(TransactionName, TransactionType,
				t =>
				{
					t.Should().NotBeNull();
					WaitHelpers.SleepMinimum();
					return 42;
				});

			res.Should().Be(42);
		});

		/// <summary>
		/// Tests the
		/// <see cref="Tracer.CaptureTransaction{T}(string,string,Func{TResult}, DistributedTracingData)" /> method
		/// with an
		/// exception.
		/// It wraps a fake transaction (Thread.Sleep) with a return value that throws an exception into the CaptureTransaction
		/// method with an <see cref="Action{ITransaction}" /> parameter
		/// and it makes sure that the transaction and the error are captured by the agent and the return value is correct and the
		/// <see cref="Action{ITransaction}" /> is not null.
		/// </summary>
		[Fact]
		public void SimpleActionWithReturnTypeAndExceptionAndParameter() => AssertWith1TransactionAnd1Error(agent =>
		{
			Action act = () =>
			{
				agent.Tracer.CaptureTransaction(TransactionName, TransactionType, t =>
				{
					t.Should().NotBeNull();
					WaitHelpers.SleepMinimum();

					if (new Random().Next(1) == 0) //avoid unreachable code warning.
						throw new InvalidOperationException(ExceptionMessage);

					return 42;
				});
				throw new Exception("CaptureTransaction should not eat exception and continue");
			};
			act.Should().Throw<InvalidOperationException>();
		});

		/// <summary>
		/// Tests the <see cref="Tracer.CaptureTransaction{T}(string,string,Func{TResult}, DistributedTracingData)" /> method with
		/// an exception.
		/// It wraps a fake transaction (Thread.Sleep) with a return value that throws an exception into the CaptureTransaction
		/// method
		/// and it makes sure that the transaction and the error are captured by the agent.
		/// </summary>
		[Fact]
		public void SimpleActionWithReturnTypeAndException() => AssertWith1TransactionAnd1Error(agent =>
		{
			Action act = () =>
			{
				agent.Tracer.CaptureTransaction(TransactionName, TransactionType, () =>
				{
					WaitHelpers.SleepMinimum();

					if (new Random().Next(1) == 0) //avoid unreachable code warning.
						throw new InvalidOperationException(ExceptionMessage);

					return 42;
				});
				throw new Exception("CaptureTransaction should not eat exception and continue");
			};
			act.Should().Throw<InvalidOperationException>();
		});

		/// <summary>
		/// Tests the <see cref="Tracer.CaptureTransaction(string,string,Func{TResult}, DistributedTracingData)" /> method.
		/// It wraps a fake async transaction (Task.Delay) into the CaptureTransaction method
		/// and it makes sure that the transaction is captured.
		/// </summary>
		[Fact]
		public async Task AsyncTask() => await AssertWith1TransactionAsync(async agent =>
		{
			await agent.Tracer.CaptureTransaction(TransactionName, TransactionType,
				async () => { await WaitHelpers.DelayMinimum(); });
		});

		/// <summary>
		/// Tests the <see cref="Tracer.CaptureTransaction(string,string,Func{TResult}, DistributedTracingData)" /> method with
		/// an exception
		/// It wraps a fake async transaction (Task.Delay) that throws an exception into the CaptureTransaction method
		/// and it makes sure that the transaction and the error are captured.
		/// </summary>
		[Fact]
		public async Task AsyncTaskWithException() => await AssertWith1TransactionAnd1ErrorAsync(async agent =>
		{
			Func<Task> act = async () =>
			{
				await agent.Tracer.CaptureTransaction(TransactionName, TransactionType, async () =>
				{
					await WaitHelpers.DelayMinimum();
					throw new InvalidOperationException(ExceptionMessage);
				});
			};
			await act.Should().ThrowAsync<InvalidOperationException>();
		});

		/// <summary>
		/// Tests the
		/// <see cref="Tracer.CaptureTransaction(string,string,Func{TResult}, DistributedTracingData)" /> method.
		/// It wraps a fake async transaction (Task.Delay) into the CaptureTransaction method with an
		/// <see cref="Action{ITransaction}" /> parameter
		/// and it makes sure that the transaction is captured and the <see cref="Action{ITransaction}" /> parameter is not null.
		/// </summary>
		[Fact]
		public async Task AsyncTaskWithParameter() => await AssertWith1TransactionAsync(async agent =>
		{
			await agent.Tracer.CaptureTransaction(TransactionName, TransactionType,
				async t =>
				{
					t.Should().NotBeNull();
					await WaitHelpers.DelayMinimum();
				});
		});

		/// <summary>
		/// Tests the
		/// <see cref="Tracer.CaptureTransaction(string,string,Func{TResult}, DistributedTracingData)" /> method
		/// with an
		/// exception.
		/// It wraps a fake async transaction (Task.Delay) that throws an exception into the CaptureTransaction method with an
		/// <see cref="Action{ITransaction}" /> parameter
		/// and it makes sure that the transaction and the error are captured and the <see cref="Action{ITransaction}" /> parameter
		/// is not null.
		/// </summary>
		[Fact]
		public async Task AsyncTaskWithExceptionAndParameter() => await AssertWith1TransactionAnd1ErrorAsync(async agent =>
		{
			Func<Task> act = async () =>
			{
				await agent.Tracer.CaptureTransaction(TransactionName, TransactionType, async t =>
				{
					t.Should().NotBeNull();
					await WaitHelpers.DelayMinimum();
					throw new InvalidOperationException(ExceptionMessage);
				});
			};
			await act.Should().ThrowAsync<InvalidOperationException>();
		});

		/// <summary>
		/// Tests the <see cref="Tracer.CaptureTransaction{T}(string,string,Func{TResult}, DistributedTracingData)" />
		/// method.
		/// It wraps a fake async transaction (Task.Delay) with a return value into the CaptureTransaction method
		/// and it makes sure that the transaction is captured by the agent and the return value is correct.
		/// </summary>
		[Fact]
		public async Task AsyncTaskWithReturnType() => await AssertWith1TransactionAsync(async agent =>
		{
			var res = await agent.Tracer.CaptureTransaction(TransactionName, TransactionType, async () =>
			{
				await WaitHelpers.DelayMinimum();
				return 42;
			});
			res.Should().Be(42);
		});

		/// <summary>
		/// Tests the
		/// <see cref="Tracer.CaptureTransaction{T}(string,string,Func{TResult}, DistributedTracingData)" />
		/// method.
		/// It wraps a fake async transaction (Task.Delay) with a return value into the CaptureTransaction method with an
		/// <see cref="Action{ITransaction}" /> parameter
		/// and it makes sure that the transaction is captured by the agent and the return value is correct and the
		/// <see cref="ITransaction" /> is not null.
		/// </summary>
		[Fact]
		public async Task AsyncTaskWithReturnTypeAndParameter() => await AssertWith1TransactionAsync(async agent =>
		{
			var res = await agent.Tracer.CaptureTransaction(TransactionName, TransactionType,
				async t =>
				{
					t.Should().NotBeNull();
					await WaitHelpers.DelayMinimum();
					return 42;
				});

			res.Should().Be(42);
		});

		/// <summary>
		/// Tests the
		/// <see cref="Tracer.CaptureTransaction{T}(string,string,Func{TResult}, DistributedTracingData)" />
		/// method with an
		/// exception.
		/// It wraps a fake async transaction (Task.Delay) with a return value that throws an exception into the CaptureTransaction
		/// method with an <see cref="Action{ITransaction}" /> parameter
		/// and it makes sure that the transaction and the error are captured by the agent and the return value is correct and the
		/// <see cref="Action{ITransaction}" /> is not null.
		/// </summary>
		[Fact]
		public async Task AsyncTaskWithReturnTypeAndExceptionAndParameter() => await AssertWith1TransactionAnd1ErrorAsync(async agent =>
		{
			Func<Task> act = async () =>
			{
				await agent.Tracer.CaptureTransaction(TransactionName, TransactionType, async t =>
				{
					t.Should().NotBeNull();
					await WaitHelpers.DelayMinimum();

					if (new Random().Next(1) == 0) //avoid unreachable code warning.
						throw new InvalidOperationException(ExceptionMessage);

					return 42;
				});
			};
			await act.Should().ThrowAsync<InvalidOperationException>();
		});

		/// <summary>
		/// Tests the <see cref="Tracer.CaptureTransaction{T}(string,string,Func{TResult}, DistributedTracingData)" />
		/// method with an exception.
		/// It wraps a fake async transaction (Task.Delay) with a return value that throws an exception into the CaptureTransaction
		/// method
		/// and it makes sure that the transaction and the error are captured by the agent.
		/// </summary>
		[Fact]
		public async Task AsyncTaskWithReturnTypeAndException() => await AssertWith1TransactionAnd1ErrorAsync(async agent =>
		{
			Func<Task> act = async () =>
			{
				await agent.Tracer.CaptureTransaction(TransactionName, TransactionType, async () =>
				{
					await WaitHelpers.DelayMinimum();

					if (new Random().Next(1) == 0) //avoid unreachable code warning.
						throw new InvalidOperationException(ExceptionMessage);

					return 42;
				});
			};
			await act.Should().ThrowAsync<InvalidOperationException>();
		});

		/// <summary>
		/// Wraps a cancelled task into the CaptureTransaction method and
		/// makes sure that the cancelled task is captured by the agent.
		/// </summary>
		[Fact]
		public async Task CancelledAsyncTask()
		{
			var payloadSender = new MockPayloadSender();
			using var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));

			var cancellationTokenSource = new CancellationTokenSource();
			var token = cancellationTokenSource.Token;
			cancellationTokenSource.Cancel();

			Func<Task> act = async () =>
			{
				await agent.Tracer.CaptureTransaction(TransactionName, TransactionType,
					async () =>
					{
						// ReSharper disable once MethodSupportsCancellation, we want to delay before we throw the exception
						await WaitHelpers.DelayMinimum();
						token.ThrowIfCancellationRequested();
					});
			};
			await act.Should().ThrowAsync<OperationCanceledException>();

			payloadSender.WaitForTransactions();
			payloadSender.Transactions.Should().NotBeEmpty();

			payloadSender.FirstTransaction.Name.Should().Be(TransactionName);
			payloadSender.FirstTransaction.Type.Should().Be(TransactionType);

			var duration = payloadSender.FirstTransaction.Duration;
			duration.Should().BeGreaterOrEqualToMinimumSleepLength();

			payloadSender.WaitForErrors();
			payloadSender.Errors.Should().NotBeEmpty();

			payloadSender.FirstError.Culprit.Should().Be("A task was canceled");
			payloadSender.FirstError.Exception.Message.Should().Be("Task canceled");
		}

		/// <summary>
		/// Creates a custom transaction and adds a label to it.
		/// Makes sure that the label is stored on Transaction.Context.
		/// </summary>
		[Fact]
		public void LabelsOnTransaction()
		{
			var payloadSender = AssertWith1Transaction(
				t =>
				{
					t.Tracer.CaptureTransaction(TransactionName, TransactionType, transaction =>
					{
						WaitHelpers.SleepMinimum();
						transaction.SetLabel("foo", "bar");
					});
				});

			//According to the Intake API labels are stored on the Context (and not on Transaction.Labels directly).
			payloadSender.FirstTransaction.Context.InternalLabels.Value.InnerDictionary["foo"].Value.Should().Be("bar");
		}

		/// <summary>
		/// Creates a custom async transaction and adds a label to it.
		/// Makes sure that the label is stored on Transaction.Context.
		/// </summary>
		[Fact]
		public async Task LabelsOnTransactionAsync()
		{
			var payloadSender = await AssertWith1TransactionAsync(
				async t =>
				{
					await t.Tracer.CaptureTransaction(TransactionName, TransactionType, async transaction =>
					{
						await WaitHelpers.DelayMinimum();
						transaction.SetLabel("foo", "bar");
					});
				});

			//According to the Intake API labels are stored on the Context (and not on Transaction.Labels directly).
			payloadSender.FirstTransaction.Context.InternalLabels.Value.MergedDictionary["foo"].Value.Should().Be("bar");
		}

		/// <summary>
		/// Creates a custom async Transaction that ends with an error and adds a label to it.
		/// Makes sure that the label is stored on Transaction.Context.
		/// </summary>
		[Fact]
		public async Task LabelsOnTransactionAsyncError()
		{
			var payloadSender = await AssertWith1TransactionAnd1ErrorAsync(
				async t =>
				{
					Func<Task> act = async () =>
					{
						await t.Tracer.CaptureTransaction(TransactionName, TransactionType, async transaction =>
						{
							await WaitHelpers.DelayMinimum();
							transaction.SetLabel("foo", "bar");

							if (new Random().Next(1) == 0) //avoid unreachable code warning.
								throw new InvalidOperationException(ExceptionMessage);
						});
					};
					await act.Should().ThrowAsync<InvalidOperationException>();
				});

			//According to the Intake API labels are stored on the Context (and not on Transaction.Labels directly).
			payloadSender.FirstTransaction.Context.InternalLabels.Value.MergedDictionary["foo"].Value.Should().Be("bar");
		}

		/// <summary>
		/// Creates a transaction and attaches a Request to this transaction.
		/// Makes sure that the transaction details are captured.
		/// </summary>
		[Fact]
		public void TransactionWithRequest()
		{
			var payloadSender = AssertWith1Transaction(
				n =>
				{
					n.Tracer.CaptureTransaction(TransactionName, TransactionType, transaction =>
					{
						WaitHelpers.SleepMinimum();
						transaction.Context.Request = new Request("GET", new Url { Protocol = "HTTP" });
					});
				});

			payloadSender.FirstTransaction.Context.Request.Method.Should().Be("GET");
			payloadSender.FirstTransaction.Context.Request.Url.Protocol.Should().Be("HTTP");
		}

		/// <summary>
		/// Creates a transaction and attaches a Request to this transaction. It fills all the fields on the Request.
		/// Makes sure that all the transaction details are captured.
		/// </summary>
		[Fact]
		public void TransactionWithRequestDetailed()
		{
			var payloadSender = AssertWith1Transaction(
				n =>
				{
					n.Tracer.CaptureTransaction(TransactionName, TransactionType, transaction =>
					{
						WaitHelpers.SleepMinimum();
						transaction.Context.Request =
							new Request("GET",
								new Url { Full = "https://elastic.co", Raw = "https://elastic.co", HostName = "elastic", Protocol = "HTTP" })
							{
								HttpVersion = "2.0",
								Socket = new Socket { RemoteAddress = "127.0.0.1" },
								Body = "123"
							};
					});
				});

			payloadSender.FirstTransaction.Context.Request.Method.Should().Be("GET");
			payloadSender.FirstTransaction.Context.Request.Url.Protocol.Should().Be("HTTP");

			payloadSender.FirstTransaction.Context.Request.HttpVersion.Should().Be("2.0");
			payloadSender.FirstTransaction.Context.Request.Url.Full.Should().Be("https://elastic.co");
			payloadSender.FirstTransaction.Context.Request.Url.Raw.Should().Be("https://elastic.co");
			payloadSender.FirstTransaction.Context.Request.Socket.RemoteAddress.Should().Be("127.0.0.1");
			payloadSender.FirstTransaction.Context.Request.Url.HostName.Should().Be("elastic");
			payloadSender.FirstTransaction.Context.Request.Body.Should().Be("123");
		}

		/// <summary>
		/// Creates a transaction and attaches a Response to this transaction.
		/// Makes sure that the transaction details are captured.
		/// </summary>
		[Fact]
		public void TransactionWithResponse()
		{
			var payloadSender = AssertWith1Transaction(
				n =>
				{
					n.Tracer.CaptureTransaction(TransactionName, TransactionType, transaction =>
					{
						WaitHelpers.SleepMinimum();
						transaction.Context.Response = new Response { Finished = true, StatusCode = 200 };
					});
				});

			payloadSender.FirstTransaction.Context.Response.Finished.Should().BeTrue();
			payloadSender.FirstTransaction.Context.Response.StatusCode.Should().Be(200);
		}

		/// <summary>
		/// Creates a transaction and attaches a Response and a request to this transaction. It sets 1 property on each.
		/// Makes sure that the transaction details are captured.
		/// </summary>
		[Fact]
		public void TransactionWithResponseAndRequest()
		{
			var payloadSender = AssertWith1Transaction(
				n =>
				{
					n.Tracer.CaptureTransaction(TransactionName, TransactionType, transaction =>
					{
						WaitHelpers.SleepMinimum();
						transaction.Context.Response = new Response { Finished = true };
						transaction.Context.Request = new Request("GET", new Url());

						transaction.Context.Response.StatusCode = 200;
						transaction.Context.Request.Url.Full = "https://elastic.co";
					});
				});

			payloadSender.FirstTransaction.Context.Response.Finished.Should().BeTrue();
			payloadSender.FirstTransaction.Context.Response.StatusCode.Should().Be(200);
			payloadSender.FirstTransaction.Context.Request.Url.Full.Should().Be("https://elastic.co");
		}

		/// <summary>
		/// Creates a transaction and captures a user on this transaction.
		/// Makes sure the user is captured by the agent.
		/// </summary>
		[Fact]
		public void TransactionWithUser()
		{
			const string userId = "123";
			const string userName = "TestUser";
			const string emailAddress = "test@user.com";

			var payloadSender = AssertWith1Transaction(
				n =>
				{
					n.Tracer.CaptureTransaction(TransactionName, TransactionType, transaction =>
					{
						WaitHelpers.SleepMinimum();
						transaction.Context.User = new User { Id = userId, UserName = userName, Email = emailAddress };
					});
				});

			payloadSender.FirstTransaction.Context.User.Id.Should().Be(userId);
			payloadSender.FirstTransaction.Context.User.UserName.Should().Be(userName);
			payloadSender.FirstTransaction.Context.User.Email.Should().Be(emailAddress);
		}

		/// <summary>
		/// Makes sure Transaction.Custom is captured and it is not truncated.
		/// </summary>
		[Fact]
		public void CaptureCustom()
		{
			var payloadSender = new MockPayloadSender();
			var customValue = "b".Repeat(10_000);
			var customKey = "a";

			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender)))
			{
				agent.Tracer.CaptureTransaction(TransactionName, TransactionType, transaction =>
				{
					transaction.Custom.Add(customKey, customValue);
					transaction.End();
				});
			}

			payloadSender.WaitForTransactions();
			payloadSender.FirstTransaction.Should().NotBeNull();
			payloadSender.FirstTransaction.Custom[customKey].Should().Be(customValue);
		}

		[Fact]
		public void CaptureErrorLogOnTransaction()
		{
			var payloadSender = new MockPayloadSender();
			using var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));

			var errorLog = new ErrorLog("foo") { Level = "error", ParamMessage = "42" };

			agent.Tracer.CaptureTransaction("foo", "bar", t => { t.CaptureErrorLog(errorLog); });

			payloadSender.WaitForAny();

			payloadSender.Transactions.Should().HaveCount(1);
			payloadSender.Spans.Should().BeNullOrEmpty();
			payloadSender.Errors.Should().HaveCount(1);
			payloadSender.FirstError.Log.Message.Should().Be("foo");
			payloadSender.FirstError.Log.Level.Should().Be("error");
			payloadSender.FirstError.Log.ParamMessage.Should().Be("42");
			payloadSender.FirstError.ParentId.Should().Be(payloadSender.FirstTransaction.Id);
		}

		[Fact]
		public async Task CaptureTransaction_Does_Not_Flow_Activity_When_Returns_Task()
		{
			Activity.Current.Should().BeNull();

			using var agent = new ApmAgent(new TestAgentComponents());

			await Agent.Tracer.CaptureTransaction("async 1", ApiConstants.TypeDb, async t =>
			{
				var activity = Activity.Current;
				activity.Should().NotBeNull();
				activity.Parent.Should().BeNull("No activity when this transaction started");

				await Task.Delay(1_000);
			});

			await Agent.Tracer.CaptureTransaction("async 2", ApiConstants.TypeDb, async () =>
			{
				var activity = Activity.Current;
				activity.Should().NotBeNull();
				activity.Parent.Should().BeNull("Activity for transaction from async 1 should not flow to here");

				await Task.Delay(1_000);
			});

			Activity.Current.Should().BeNull();
		}

		/// <summary>
		/// Asserts on 1 transaction with async code
		/// </summary>
		private async Task<MockPayloadSender> AssertWith1TransactionAsync(Func<ApmAgent, Task> func)
		{
			var payloadSender = new MockPayloadSender();
			using var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));

			await func(agent);

			payloadSender.WaitForTransactions();
			payloadSender.Transactions.Should().NotBeEmpty();

			payloadSender.FirstTransaction.Name.Should().Be(TransactionName);
			payloadSender.FirstTransaction.Type.Should().Be(TransactionType);

			var duration = payloadSender.FirstTransaction.Duration;
			duration.Should().BeGreaterOrEqualToMinimumSleepLength();

			return payloadSender;
		}

		/// <summary>
		/// Asserts on 1 async transaction and 1 error
		/// </summary>
		private async Task<MockPayloadSender> AssertWith1TransactionAnd1ErrorAsync(Func<ApmAgent, Task> func)
		{
			var payloadSender = new MockPayloadSender();
			using var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));

			await func(agent);

			payloadSender.WaitForTransactions();
			payloadSender.Transactions.Should().NotBeEmpty();

			payloadSender.FirstTransaction.Name.Should().Be(TransactionName);
			payloadSender.FirstTransaction.Type.Should().Be(TransactionType);

			var duration = payloadSender.FirstTransaction.Duration;
			duration.Should().BeGreaterOrEqualToMinimumSleepLength();

			payloadSender.WaitForErrors();
			payloadSender.Errors.Should().NotBeEmpty();

			payloadSender.FirstError.Exception.Type.Should().Be(typeof(InvalidOperationException).FullName);
			payloadSender.FirstError.Exception.Message.Should().Be(ExceptionMessage);
			return payloadSender;
		}

		/// <summary>
		/// Asserts on 1 transaction
		/// </summary>
		private MockPayloadSender AssertWith1Transaction(Action<ApmAgent> action)
		{
			var payloadSender = new MockPayloadSender();
			using var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));

			action(agent);

			payloadSender.WaitForTransactions();
			payloadSender.Transactions.Should().NotBeEmpty();
			payloadSender.Transactions.Should().NotBeEmpty();

			payloadSender.FirstTransaction.Name.Should().Be(TransactionName);
			payloadSender.FirstTransaction.Type.Should().Be(TransactionType);

			var duration = payloadSender.FirstTransaction.Duration;
			duration.Should().BeGreaterOrEqualToMinimumSleepLength();

			return payloadSender;
		}

		/// <summary>
		/// Asserts on 1 transaction and 1 error
		/// </summary>
		private void AssertWith1TransactionAnd1Error(Action<ApmAgent> action)
		{
			var payloadSender = new MockPayloadSender();
			using var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));

			action(agent);

			payloadSender.WaitForTransactions();
			payloadSender.Transactions.Should().NotBeEmpty();

			payloadSender.FirstTransaction.Name.Should().Be(TransactionName);
			payloadSender.FirstTransaction.Type.Should().Be(TransactionType);

			var duration = payloadSender.FirstTransaction.Duration;
			duration.Should().BeGreaterOrEqualToMinimumSleepLength();

			payloadSender.WaitForErrors();
			payloadSender.Errors.Should().NotBeEmpty();

			payloadSender.FirstError.Exception.Type.Should().Be(typeof(InvalidOperationException).FullName);
			payloadSender.FirstError.Exception.Message.Should().Be(ExceptionMessage);
		}
	}
}
