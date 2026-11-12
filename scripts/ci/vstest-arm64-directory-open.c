/* SDK 10.0.404 (Microsoft.TestPlatform.CrossPlatEngine, GetOpenNoFollowDirectoryFlags)
   returns 196608 on every operating system except macOS. That is O_NOFOLLOW|O_DIRECTORY
   in the generic Linux header, which is what x86_64 uses. On aarch64 the same bits are
   O_LARGEFILE|O_DIRECT, and open() of a directory with O_DIRECT fails with EINVAL
   (errno 22) whether the directory is mode 0700 or 0755. The real aarch64 values are
   O_DIRECTORY=0x4000 and O_NOFOLLOW=0x8000.

   vstest then never reaches the fchmod that would make the session directory owner-only,
   and the later check ("can be accessed by users other than its owner") fails the run.
   Rewriting this one flag value lets that check run and pass. No other open() is changed,
   and VSTEST_DISABLE_ARTIFACTS_DIRECTORY_VERIFICATION is not set. */

#define _GNU_SOURCE
#include <dlfcn.h>
#include <fcntl.h>
#include <stdarg.h>
#include <sys/types.h>

#define VSTEST_X64_DIRECTORY_FLAGS 196608

static int (*real_open)(const char *, int, ...);

static void resolve_open(void) __attribute__((constructor));
static void resolve_open(void)
{
    real_open = dlsym(RTLD_NEXT, "open");
}

int open(const char *pathname, int flags, ...)
{
    if (flags == VSTEST_X64_DIRECTORY_FLAGS)
    {
        flags = O_DIRECTORY | O_NOFOLLOW;
    }

    if (flags & O_CREAT)
    {
        va_list ap;
        mode_t mode;
        va_start(ap, flags);
        mode = va_arg(ap, mode_t);
        va_end(ap);
        return real_open(pathname, flags, mode);
    }

    return real_open(pathname, flags);
}
