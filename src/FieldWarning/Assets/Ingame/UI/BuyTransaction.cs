﻿/**
* Copyright (c) 2017-present, PFW Contributors.
*
* Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in
* compliance with the License. You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software distributed under the License is
* distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See
* the License for the specific language governing permissions and limitations under the License.
*/

using System.Collections.Generic;

namespace Assets.Ingame.UI {
    public class BuyTransaction {
        private GhostPlatoonBehaviour _ghostPlatoonBehaviour;
        private readonly List<GhostPlatoonBehaviour> _ghostUnits;

        private const int MAX_PLATOON_SIZE = 4;
        private const int MIN_PLATOON_SIZE = 1;

        private int _smallestPlatoonSize;

        public UnitType UnitType { get; }
        public Player Owner { get; }

        //public event Action<GhostPlatoonBehaviour> Finished;

        //public void OnFinished(GhostPlatoonBehaviour behaviour)
        //{
        //    Finished?.Invoke(behaviour);
        //}

        public BuyTransaction(UnitType type, Player owner, List<GhostPlatoonBehaviour> ghostUnits)
        {
            UnitType = type;
            Owner = owner;

            _smallestPlatoonSize = MIN_PLATOON_SIZE;
            _ghostPlatoonBehaviour = 
                GhostPlatoonBehaviour.build(type, owner, _smallestPlatoonSize);

            _ghostUnits = ghostUnits;
            _ghostUnits.Add(_ghostPlatoonBehaviour);
        }

        public void AddUnit()
        {
            if (_smallestPlatoonSize < MAX_PLATOON_SIZE) {

                _ghostPlatoonBehaviour.AddSingleUnit();
                _ghostPlatoonBehaviour.buildRealPlatoon();
                _smallestPlatoonSize++;
            } else {

                // If all platoons in the transaction are max size, we add a new one and update the size counter:
                _smallestPlatoonSize = MIN_PLATOON_SIZE;
                _ghostPlatoonBehaviour = GhostPlatoonBehaviour.build(UnitType, Owner, _smallestPlatoonSize);
                _ghostUnits.Add(_ghostPlatoonBehaviour);
            }
        }
    }
}
