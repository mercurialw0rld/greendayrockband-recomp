// Green Day enters its frame loop through an Xbox exception callback.
// At 0x82200610 the game installs 0x822005E8, then deliberately stores to zero.
// The callback restores the previous handler and calls 0x82200238 with the
// context originally passed to 0x82200610. Invoke that continuation directly
// so the host never needs to dispatch this synthetic Xbox exception.
#include <rex/hook.h>

extern "C" REX_FUNC(sub_82200238);

REX_HOOK_RAW(sub_82200610) {
  sub_82200238(ctx, base);
}
