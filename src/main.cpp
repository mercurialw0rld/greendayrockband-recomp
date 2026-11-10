// greendayrockband - ReXGlue Recompiled Project

#include "generated/default/greendayrockband_init.h"

#include "greendayrockband_app.h"
REXCVAR_DEFINE_BOOL(achievement_popups, false, "UI", "Show optional achievement popups (continuous overlay rendering)")
    .lifecycle(rex::cvar::Lifecycle::kInitOnly);

REX_DEFINE_APP(greendayrockband, GreenDayRockbandApp::Create)

