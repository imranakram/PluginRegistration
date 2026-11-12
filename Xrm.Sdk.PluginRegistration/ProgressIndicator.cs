// =====================================================================
//
//  This file is part of the Microsoft Dynamics CRM SDK code samples.
//
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//
//  This source code is intended only as a supplement to Microsoft
//  Development Tools and/or on-line documentation.  See these other
//  materials for detailed information regarding Microsoft code samples.
//
//  THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
//  KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//  IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
//  PARTICULAR PURPOSE.
//
// =====================================================================

namespace Xrm.Sdk.PluginRegistration
{
    using System;
    using XrmToolBox.Extensibility.Args;

    public delegate void ProgressIndicatorAppendStatusText(string text);

    public delegate void ProgressIndicatorComplete();

    public delegate void ProgressIndicatorIncrement(int incrementBy);

    public delegate void ProgressIndicatorInitialize(int min, int max, int initialValue);

    public delegate void ProgressIndicatorSetStatusText(string text);

    public delegate void ProgressIndicatorSetValue(int value);

    public class ProgressIndicator
    {
        #region Private Fields

        private ProgressIndicatorAppendStatusText m_appendText;
        private ProgressIndicatorComplete m_complete;
        private ProgressIndicatorIncrement m_increment;
        private ProgressIndicatorInitialize m_init;
        private ProgressIndicatorSetStatusText m_setText;
        private ProgressIndicatorSetValue m_setValue;

        #endregion Private Fields

        #region Public Constructors

        public ProgressIndicator(ProgressIndicatorInitialize init, ProgressIndicatorComplete complete,
            ProgressIndicatorAppendStatusText appendText, ProgressIndicatorSetStatusText setText,
            ProgressIndicatorIncrement increment, ProgressIndicatorSetValue setValue)
        {
            if (init == null)
            {
                throw new ArgumentNullException("init");
            }

            m_init = init;
            m_appendText = appendText;
            m_setText = setText;
            m_setValue = setValue;
            m_increment = increment;
            m_complete = complete;
        }

        public ProgressIndicator(Action<StatusBarMessageEventArgs> action)
        {
            // Units of work of the current run. Counted here because Step rounds down to 0 for
            // more than 100 units, which would never move the bar.
            int total = 0;
            int done = 0;
            string text = null;
            Func<int> percent = () => total <= 0 ? 0 : (int)Math.Floor((decimal)Math.Min(done, total) * 100 / total);

            m_init = delegate (int min, int max, int initial)
            {
                if (min < 0 || max <= 0)
                {
                    return;
                }

                Min = 0;
                Max = 100;
                Step = (int)Math.Floor((decimal)100 / max);
                Initial = (int)Math.Round((decimal)initial / max * 100);
                total = max;
                done = initial;
                text = null;

                action(new StatusBarMessageEventArgs(Initial));
            };

            m_increment = delegate (int incrementBy)
            {
                if (total <= 0)
                {
                    return;
                }

                // Every caller advances one unit of work at a time
                done++;
                action(text == null
                    ? new StatusBarMessageEventArgs(percent())
                    : new StatusBarMessageEventArgs(percent(), text));
            };

            m_setText = delegate (string message)
            {
                text = string.IsNullOrEmpty(message) ? null : message;

                // During a run, send the progress along so the status bar can show both
                action(total > 0 && text != null
                    ? new StatusBarMessageEventArgs(percent(), text)
                    : new StatusBarMessageEventArgs(message));
            };

            m_complete = delegate ()
            {
                total = 0;
                done = 0;
                text = null;
                action(new StatusBarMessageEventArgs(string.Empty));
            };
        }

        #endregion Public Constructors

        #region Private Enums

        private enum MethodType
        {
            Initialize,
            SetValue,
            Increment,
            AppendStatusText,
            SetStatusText,
            Complete
        }

        #endregion Private Enums

        #region Public Properties

        public int Initial { get; private set; }
        public int Max { get; private set; }
        public int Min { get; private set; }
        public int Step { get; private set; }

        #endregion Public Properties

        #region Public Methods

        public void AppendText(string text)
        {
            if (m_setText == null && m_appendText == null)
            {
                return;
            }

            if (m_appendText != null && !string.IsNullOrEmpty(text))
            {
                m_appendText(text);
            }
            else if (m_setText != null)
            {
                m_setText(text);
            }

            //UI doesn't always refresh properly. This will allow the UI to refresh
            System.Windows.Forms.Application.DoEvents();
        }

        public void ClearText()
        {
            SetText(string.Empty);
        }

        public void Complete()
        {
            Complete(false);
        }

        public void Complete(bool clearStatusText)
        {
            if (clearStatusText)
            {
                SetText(string.Empty);
            }

            if (m_complete != null)
            {
                m_complete();
            }

            //UI doesn't always refresh properly. This will allow the UI to refresh
            System.Windows.Forms.Application.DoEvents();
        }

        public void Increment()
        {
            Increment(Step, null);
        }

        public void Increment(string message)
        {
            Increment(Step, message);
        }

        public void Increment(int value)
        {
            Increment(value, null);
        }

        public void Increment(int value, string message)
        {
            if (m_increment != null)
            {
                m_increment(value);
            }

            if (message != null)
            {
                AppendText(message);
            }

            //UI doesn't always refresh properly. This will allow the UI to refresh
            System.Windows.Forms.Application.DoEvents();
        }

        public void Initialize(int max)
        {
            Initialize(max, (string)null);
        }

        public void Initialize(int max, string initialStatusText)
        {
            Initialize(0, max, null, initialStatusText);
        }

        public void Initialize(int max, int? initialValue)
        {
            Initialize(0, max, initialValue);
        }

        public void Initialize(int min, int max, int? initialValue)
        {
            Initialize(min, max, initialValue, null);
        }

        public void Initialize(int min, int max, int? initialValue, string initialStatusText)
        {
            if (min > max)
            {
                throw new ArgumentOutOfRangeException("Minimum cannot be greater than maximum");
            }
            else if (initialValue != null && (initialValue < min || initialValue > max))
            {
                throw new ArgumentOutOfRangeException("Cannot be less than Minimum or greater than Maximum", "initialValue");
            }

            int value;
            if (initialValue == null)
            {
                value = min;
            }
            else
            {
                value = (int)initialValue;
            }

            m_init(min, max, value);

            if (initialStatusText != null)
            {
                SetText(initialStatusText);
            }

            //UI doesn't always refresh properly. This will allow the UI to refresh
            System.Windows.Forms.Application.DoEvents();
        }

        public void SetText(string text)
        {
            if (m_setText == null && m_appendText == null)
            {
                return;
            }

            if (m_setText != null)
            {
                m_setText(text);
            }
            else if (null != m_appendText && !string.IsNullOrEmpty(text))
            {
                m_appendText(text);
            }

            //UI doesn't always refresh properly. This will allow the UI to refresh
            System.Windows.Forms.Application.DoEvents();
        }

        #endregion Public Methods
    }
}