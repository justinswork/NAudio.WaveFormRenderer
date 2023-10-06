using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace WaveformRenderer_NetCore
{
	public class SimpleCommand : ICommand, INotifyPropertyChanged
	{
		// see important notes accompanying CanExecuteChanged
		private readonly bool _requerySuggested;
		protected Predicate<object> _canExecuteDelegate;
		protected Action<object> _executeDelegate;

		protected SimpleCommand() : this(
			executeDelegate: default
			)
		{ }

		public SimpleCommand(
			Action<object> executeDelegate,
			Predicate<object> canExecuteDelegate = default,
			bool? requerySuggested = null
			)
		{
			_requerySuggested = requerySuggested ?? canExecuteDelegate != null;
			_canExecuteDelegate = canExecuteDelegate;
			_executeDelegate = executeDelegate;
		}

		public SimpleCommand(
			Action execute,
			Func<bool> canExecute = default,
			bool? requerySuggested = null
			) : this(
				o => execute(),
				canExecute != default ? (Predicate<object>)(o => canExecute()) : default,
				requerySuggested
				)
		{ }

		private bool _canExecute;

		public bool CanExecuteResult
		{
			get { return _canExecute; }
			set { _canExecute = value; OnPropertyChanged(); }
		}


		public bool CanExecute(object parameter)
		{
			CanExecuteResult = _canExecuteDelegate != null
				? _canExecuteDelegate(parameter)
				: true;

			// if there is no can execute default to true
			return CanExecuteResult;
		}

		public event EventHandler CanExecuteChanged
		{
			add
			{
				if (_requerySuggested)
					CommandManager.RequerySuggested += value;
			}
			remove
			{
				// this may cause a leak by retaining an eventhandler reference if the flag is changed.
				// this is why _requerySuggested needs to stay private and not change during usage.
				if (_requerySuggested)
					CommandManager.RequerySuggested -= value;
			}
		}

		public void Execute(object parameter)
		{
			_executeDelegate?.Invoke(parameter);
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
