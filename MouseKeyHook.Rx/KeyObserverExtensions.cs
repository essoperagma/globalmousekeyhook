﻿using System;
using System.Collections.Generic;
using System.Linq;
using Gma.System.MouseKeyHook;
using srx=System.Reactive.Linq;
using System.Reactive.Linq;
using System.Windows.Forms;
using Gma.System.MouseKeyHook.Implementation;

namespace MouseKeyHook.Rx
{
    public static class KeyObserverExtensions
    {
        public static IObservable<Keys> KeyDownObservable(this IKeyboardEvents source)
        {
            return srx
                .Observable
                .FromEventPattern<KeyEventArgs>(source, "KeyDown")
                .Select(ep => ep.EventArgs.KeyCode);
        }

        public static IObservable<Keys> KeyUpObservable(this IKeyboardEvents source)
        {
            return srx
                .Observable
                .FromEventPattern<KeyEventArgs>(source, "KeyDown")
                .Select(ep => ep.EventArgs.KeyCode);
        }


        public static IObservable<KeyEvent> UpDownEvents(this IKeyboardEvents source)
        {
            return source
                    .KeyDownObservable()
                    .Select(key=>key.Down())
                    .Merge(source
                        .KeyUpObservable()
                        .Select(key=>key.Down()));
        }


        public static IObservable<KeyWithState> WithState(this IObservable<Keys> source)
        {
            return source
                .Select(evt => new KeyWithState(evt, KeyboardState.GetCurrent()));
        }

        public static IObservable<Trigger> Matching(this IObservable<Keys> source, IEnumerable<Trigger> triggers)
        {
            return source
                .WithState()
                .SelectMany(se => triggers.Where(se.Matches));
        }

        public static IObservable<Trigger> MatchingLongest(this IObservable<Keys> source, IEnumerable<Trigger> triggers)
        {
            var sortedTriggers = triggers
                .GroupBy(t => t.TriggerKey)
                .Select(group => new KeyValuePair<Keys, IEnumerable<Trigger>>(group.Key, group.OrderBy(t => -t.Length)))
                .ToDictionary(pair=>pair.Key, pair=>pair.Value);

            return source
                .Where(keyCode=> sortedTriggers.ContainsKey(keyCode))
                .WithState()
                .Select(se => sortedTriggers[se.KeyCode].First(se.Matches));
        }

        public static IObservable<IEnumerable<KeyEvent>> Sequences(this IObservable<KeyEvent> source, int minLength, int maxLength)
        {
            return Enumerable
                .Range(minLength, maxLength)
                .Select(n => source
                    .Buffer(n, 1))
                .Merge();
        }

        public static IObservable<KeySequence> Match(this IObservable<KeyEvent> upDownObservable, IEnumerable<KeySequence> whitelist)
        {
            var min = whitelist.Select(e => e.Count()).Min();
            var max = whitelist.Select(e => e.Count()).Max();

            return upDownObservable.Sequences(min, max)
                .Select(sequence=>whitelist.Where(w=>w.SequenceEqual(sequence)))
                .SelectMany(secs=>secs);
        }

    }
}
