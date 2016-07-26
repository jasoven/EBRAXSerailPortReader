using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;


namespace EBRAXRS232Service
{
    [RunInstaller(true)]
    public partial class EBRAXRS232SrvInstaller : System.Configuration.Install.Installer
    {
        public EBRAXRS232SrvInstaller()
        {
            InitializeComponent();
        }
    }
}
