import com.itextpdf.tool.xml.*;

/**
 * Created with IntelliJ IDEA.
 * User: Jens
 * Date: 22/10/12
 * Time: 10:54
 * To change this template use File | Settings | File Templates.
 */
public class PipelineTest {
    public static void main(String[] args){

    }

    public interface Pipeline<T extends CustomContext> {


        Pipeline init(final WorkerContext context) throws PipelineException;

        Pipeline open(WorkerContext context, Tag t, ProcessObject po) throws PipelineException;

        Pipeline content(WorkerContext context, Tag t, byte[] content, ProcessObject po) throws PipelineException;

        Pipeline close(WorkerContext context, Tag t, ProcessObject po) throws PipelineException;

        Pipeline getNext();


    }
    private class TagTest implements Pipeline<CustomContext>{

        @Override
        public Pipeline init(WorkerContext context) throws PipelineException {
            return null;  //To change body of implemented methods use File | Settings | File Templates.
        }

        @Override
        public Pipeline open(WorkerContext context, Tag t, ProcessObject po) throws PipelineException {
            return null;  //To change body of implemented methods use File | Settings | File Templates.
        }

        @Override
        public Pipeline content(WorkerContext context, Tag t, byte[] content, ProcessObject po) throws PipelineException {
            return null;  //To change body of implemented methods use File | Settings | File Templates.
        }

        @Override
        public Pipeline close(WorkerContext context, Tag t, ProcessObject po) throws PipelineException {
            return null;  //To change body of implemented methods use File | Settings | File Templates.
        }

        @Override
        public Pipeline getNext() {
            return null;  //To change body of implemented methods use File | Settings | File Templates.
        }
    }
}
