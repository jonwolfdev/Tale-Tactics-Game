﻿using Assets.Scripts.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts
{
    public static class Global
    {
        static CurrentGameConfigurationModel readGameConfigurationModel;
        public static CurrentGameConfigurationModel GetGameConfiguration()
        {
            // TODO: should copy model?
            return readGameConfigurationModel;
        }

        public static void SetGameConfiguration(CurrentGameConfigurationModel model)
        {
            // TODO: should copy the model?
            readGameConfigurationModel = model;
        }
    }
}
