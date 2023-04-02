/*
 * Copyright (c) 2023 Yilin from NOC. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
using System.Collections;

namespace NRegEx;

public interface Lookups<TKey, TValue>
    : IEnumerable<KeyValuePair<TKey, ICollection<TValue>>>,
      ICollection<KeyValuePair<TKey, ICollection<TValue>>>,
      IDictionary<TKey, ICollection<TValue>>,
      IEnumerable,
      ICollection
    where TKey : notnull
{ }
