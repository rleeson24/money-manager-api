using AutoFixture;
using AutoFixture.Kernel;
using Moq;
using System.Reflection;

namespace MoneyManager.Tests.Utilities;

/// <summary>
/// Base class that builds the subject under test via reflection: discovers constructor dependencies,
/// creates Moq mocks or concrete instances for each, and constructs the subject.
/// Use <see cref="HandlerBase{TSubject}"/> for MediatR handler tests (adds <see cref="IAsyncLifetime"/> and fixture lifecycle).
/// </summary>
/// <typeparam name="TSubject">The class under test (e.g. UpdateExpenseHandler).</typeparam>
public abstract class TestBase<TSubject>
	where TSubject : class
{
	private readonly Dictionary<Type, object> _mocks = new();
	private readonly Dictionary<Type, object> _concreteObjects = new();
	private TSubject? _subjectUnderTest;

	/// <summary>
	/// AutoFixture instance for generating anonymous test data (Guids, strings, dates, etc.).
	/// </summary>
	protected IFixture Fixture { get; }

	/// <summary>
	/// The instance of the class under test, created after mocks are built.
	/// When using <see cref="TestBase{TSubject}"/> directly (sync tests), configure mocks and data in the derived constructor, then call <see cref="BuildSubject"/> and optionally <see cref="ExecuteTestMethod"/>; or the subject is built on first access.
	/// </summary>
	protected TSubject SubjectUnderTest => _subjectUnderTest ??= CreateSubject();

	/// <summary>
	/// Exposes the mock for a dependency type so the child class can set up methods (e.g. Repository.Setup(...)).
	/// </summary>
	protected Mock<T> MockFor<T>() where T : class
	{
		var type = typeof(T);
		if (_mocks.TryGetValue(type, out var mock) && mock is Mock<T> typed)
			return typed;
		throw new InvalidOperationException($"No mock registered for type {type.Name}. Ensure {typeof(TSubject).Name} has a constructor parameter of type {type.Name}.");
	}

	/// <summary>
	/// Exposes the object for a dependency type so the child class can prepare for test.
	/// </summary>
	protected T ObjectFor<T>() where T : class
	{
		var type = typeof(T);
		if (_concreteObjects.TryGetValue(type, out var obj) && obj is T typed)
			return typed;
		throw new InvalidOperationException($"No object registered for type {type.Name}. Ensure {typeof(TSubject).Name} has a constructor parameter of type {type.Name}.");
	}

	/// <summary>
	/// Override to provide a custom instance for a concrete (non-interface) dependency type
	/// instead of using AutoFixture. Return null to use the default Fixture.Create behavior.
	/// </summary>
	protected virtual object? CreateConcreteInstance(Type type) => null;

	/// <summary>
	/// Override in the child class to run after Subject is created (e.g. call handler once and store result).
	/// Used by <see cref="HandlerBase{TSubject}"/> (async lifecycle).
	/// </summary>
	protected virtual Task ExecuteTestMethodAsync() => Task.CompletedTask;

	/// <summary>
	/// Override in the child class to run after Subject is built (sync). Use for mapper-style tests that store one-time results; call from derived constructor after <see cref="BuildSubject"/>.
	/// </summary>
	protected virtual void ExecuteTestMethod() { }

	/// <summary>
	/// Builds the subject under test from the configured mocks and concrete dependencies. Call from derived constructor or <see cref="HandlerBase{TSubject}.InitializeAsync"/> after configuring mocks and test data.
	/// </summary>
	protected void BuildSubject()
	{
		CreateSubject();
	}

	protected TestBase()
	{
		Fixture = ConfiguredFixture.Create();
		BuildMocks();
	}

	private void BuildMocks()
	{
		var subjectType = typeof(TSubject);
		var ctor = GetConstructor(subjectType);

		var fixtureCreate = typeof(SpecimenFactory).GetMethods(BindingFlags.Public | BindingFlags.Static)
			.FirstOrDefault(m => m.Name == "Create" && m.IsGenericMethodDefinition
				&& m.GetParameters().Length == 1
				&& m.GetParameters()[0].ParameterType == typeof(ISpecimenBuilder));

		foreach (var parameter in ctor.GetParameters())
		{
			var paramType = parameter.ParameterType;
			if (CanMock(paramType))
			{
				var mockType = typeof(Mock<>).MakeGenericType(paramType);
				var mockCtor = mockType.GetConstructor(new[] { typeof(MockBehavior) });
				var mock = mockCtor?.Invoke(new object[] { MockBehavior.Loose });
				if (mock != null)
					_mocks[paramType] = mock;
			}
			else
			{
				var custom = CreateConcreteInstance(paramType);
				object obj;
				if (custom != null)
				{
					obj = custom;
				}
				else
				{
					var genericCreate = fixtureCreate?.MakeGenericMethod(paramType);
					obj = genericCreate?.Invoke(null, new object[] { Fixture })!;
					var emailProps = paramType.GetProperties().Where(p => p.PropertyType == typeof(string) && p.Name.Contains("email", StringComparison.InvariantCultureIgnoreCase)).ToList();
					if (emailProps.Any())
					{
						foreach (var prop in emailProps)
						{
							if (prop.CanWrite)
								prop.SetValue(obj, Fixture.Create<string>() + "@example.com");
						}
					}
				}
				_concreteObjects[paramType] = obj;
			}
		}
	}

	private static ConstructorInfo GetConstructor(Type type)
	{
		var ctors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
		if (ctors.Length == 0)
			throw new InvalidOperationException($"{type.Name} has no public instance constructor.");
		return ctors[0];
	}

	private static bool CanMock(Type type) => type.IsInterface;

	private TSubject CreateSubject()
	{
		var subjectType = typeof(TSubject);
		var ctor = GetConstructor(subjectType);
		var parameters = ctor.GetParameters()
			.Select(p => ResolveParameter(p.ParameterType))
			.ToArray();

		_subjectUnderTest = (TSubject)ctor.Invoke(parameters);
		return _subjectUnderTest;
	}

	private object? ResolveParameter(Type parameterType)
	{
		if (_mocks.TryGetValue(parameterType, out var mock))
		{
			var objectProperty = mock.GetType().GetProperty("Object", parameterType);
			return objectProperty?.GetValue(mock);
		}

		if (_concreteObjects.TryGetValue(parameterType, out var obj))
			return obj;

		if (parameterType == typeof(CancellationToken))
			return default(CancellationToken);

		if (parameterType.IsValueType)
			return Activator.CreateInstance(parameterType);

		return null;
	}
}
